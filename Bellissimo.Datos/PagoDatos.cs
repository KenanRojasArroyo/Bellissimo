using System.Data;
using Bellissimo.Entidades;
using MySql.Data.MySqlClient;

namespace Bellissimo.Datos;

// Pagos semanales. La aplicación NO calcula ni escribe pagos directamente:
// usa sp_generar_pago, sp_ticket, sp_cerrar_pago y sp_agregar_observacion,
// y lee v_resumen_pago_semanal y v_dias_sin_checada (Sql/CONTRATO_BD.md, regla 0).
public class PagoDatos
{
    private const string SelectResumen = """
        SELECT id_pago, id_semana, fecha_inicio, fecha_fin, id_empleado,
               empleado, puesto, sueldo_base, descuento_total, monto_extra,
               salario_final, estado
        FROM v_resumen_pago_semanal
        """;

    // Filtros opcionales: null = sin filtro. estado: Borrador o Cerrado.
    public List<ResumenPago> ListarResumen(int? idSemana, int? idEmpleado, string? estado)
    {
        List<ResumenPago> lista = new();

        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectResumen + """

            WHERE (@idSemana IS NULL OR id_semana = @idSemana)
              AND (@idEmpleado IS NULL OR id_empleado = @idEmpleado)
              AND (@estado IS NULL OR estado = @estado)
            ORDER BY fecha_inicio DESC, empleado;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idSemana", (object?)idSemana ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@idEmpleado", (object?)idEmpleado ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@estado", (object?)estado ?? DBNull.Value);

        using MySqlDataReader dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            lista.Add(MapearResumen(dr));
        }

        return lista;
    }

    public ResumenPago? BuscarResumen(int idEmpleado, int idSemana)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectResumen + """

            WHERE id_empleado = @idEmpleado
              AND id_semana = @idSemana;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idEmpleado", idEmpleado);
        cmd.Parameters.AddWithValue("@idSemana", idSemana);

        using MySqlDataReader dr = cmd.ExecuteReader();

        return dr.Read() ? MapearResumen(dr) : null;
    }

    public ResumenPago? BuscarResumenPorId(int idPago)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectResumen + """

            WHERE id_pago = @idPago;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idPago", idPago);

        using MySqlDataReader dr = cmd.ExecuteReader();

        return dr.Read() ? MapearResumen(dr) : null;
    }

    // Se llama en modo autocommit, sin MySqlTransaction (regla 4 / INC-04):
    // el procedimiento abre su propia transacción.
    public void Generar(int idEmpleado, int idSemana, int? idUsuario)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        using MySqlCommand cmd = new("sp_generar_pago", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("p_id_empleado", idEmpleado);
        cmd.Parameters.AddWithValue("p_id_semana", idSemana);
        cmd.Parameters.AddWithValue("p_id_usuario", (object?)idUsuario ?? DBNull.Value);

        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (BaseDatosException.EsConocido(ex))
        {
            throw BaseDatosException.Desde(ex);
        }
    }

    // sp_ticket devuelve 2 resultados: resumen (1 fila) y renglones (0..N).
    public Ticket ObtenerTicket(int idEmpleado, int idSemana)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        using MySqlCommand cmd = new("sp_ticket", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("p_id_empleado", idEmpleado);
        cmd.Parameters.AddWithValue("p_id_semana", idSemana);

        try
        {
            using MySqlDataReader dr = cmd.ExecuteReader();
            dr.Read();

            Ticket ticket = new()
            {
                IdEmpleado = idEmpleado,
                IdSemana = idSemana,
                Empleado = Convert.ToString(dr["empleado"]) ?? "",
                Puesto = Convert.ToString(dr["puesto"]) ?? "",
                FechaInicio = Convert.ToDateTime(dr["fecha_inicio"]),
                FechaFin = Convert.ToDateTime(dr["fecha_fin"]),
                SueldoBase = Convert.ToDecimal(dr["sueldo_base"]),
                ValorHora = Convert.ToDecimal(dr["valor_hora"]),
                MinExcesoComida = Convert.ToInt32(dr["min_exceso_comida"]),
                MinRetardo = Convert.ToInt32(dr["min_retardo"]),
                MinSalidaTemprana = Convert.ToInt32(dr["min_salida_temprana"]),
                MinFalta = Convert.ToInt32(dr["min_falta"]),
                MinExtra = Convert.ToInt32(dr["min_extra"]),
                DescuentoTotal = Convert.ToDecimal(dr["descuento_total"]),
                MontoExtra = Convert.ToDecimal(dr["monto_extra"]),
                SalarioAPagar = Convert.ToDecimal(dr["salario_a_pagar"]),
                Estado = Convert.ToString(dr["estado"]) ?? "",
                FechaGeneracion = Convert.ToDateTime(dr["fecha_generacion"]),
                GeneradoPor =
                    dr["generado_por"] == DBNull.Value
                        ? null
                        : Convert.ToString(dr["generado_por"]),
                Observaciones =
                    dr["observaciones"] == DBNull.Value
                        ? null
                        : Convert.ToString(dr["observaciones"])
            };

            dr.NextResult();

            while (dr.Read())
            {
                ticket.Renglones.Add(new RenglonTicket
                {
                    Fecha =
                        dr["fecha"] == DBNull.Value
                            ? null
                            : Convert.ToDateTime(dr["fecha"]),
                    Concepto = Convert.ToString(dr["concepto"]) ?? "",
                    Minutos = Convert.ToInt32(dr["minutos"]),
                    Monto = Convert.ToDecimal(dr["monto"])
                });
            }

            return ticket;
        }
        catch (MySqlException ex) when (BaseDatosException.EsConocido(ex))
        {
            throw BaseDatosException.Desde(ex);
        }
    }

    public void Cerrar(int idPago)
    {
        EjecutarProcedimiento(
            "sp_cerrar_pago",
            ("p_id_pago", idPago));
    }

    public void AgregarObservacion(int idPago, string? texto)
    {
        EjecutarProcedimiento(
            "sp_agregar_observacion",
            ("p_id_pago", idPago),
            ("p_texto", (object?)texto ?? DBNull.Value));
    }

    // Días laborales sin checada ni ausencia (aviso antes de cerrar).
    public List<DiaSinChecada> ListarDiasSinChecada(int idSemana, int? idEmpleado)
    {
        List<DiaSinChecada> lista = new();

        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT id_semana, id_empleado, empleado, fecha, dia_semana,
                   minutos_jornada, semana_cerrada
            FROM v_dias_sin_checada
            WHERE id_semana = @idSemana
              AND (@idEmpleado IS NULL OR id_empleado = @idEmpleado)
            ORDER BY fecha, empleado;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idSemana", idSemana);
        cmd.Parameters.AddWithValue("@idEmpleado", (object?)idEmpleado ?? DBNull.Value);

        using MySqlDataReader dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            lista.Add(new DiaSinChecada
            {
                IdSemana = Convert.ToInt32(dr["id_semana"]),
                IdEmpleado = Convert.ToInt32(dr["id_empleado"]),
                Empleado = Convert.ToString(dr["empleado"]) ?? "",
                Fecha = Convert.ToDateTime(dr["fecha"]),
                DiaSemana = Convert.ToInt32(dr["dia_semana"]),
                MinutosJornada = Convert.ToInt32(dr["minutos_jornada"]),
                SemanaCerrada = Convert.ToInt32(dr["semana_cerrada"]) == 1
            });
        }

        return lista;
    }

    private static void EjecutarProcedimiento(
        string nombre,
        params (string Nombre, object Valor)[] parametros)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        using MySqlCommand cmd = new(nombre, cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        foreach (var (nombreParametro, valor) in parametros)
        {
            cmd.Parameters.AddWithValue(nombreParametro, valor);
        }

        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (BaseDatosException.EsConocido(ex))
        {
            throw BaseDatosException.Desde(ex);
        }
    }

    private static ResumenPago MapearResumen(MySqlDataReader dr)
    {
        return new ResumenPago
        {
            IdPago = Convert.ToInt32(dr["id_pago"]),
            IdSemana = Convert.ToInt32(dr["id_semana"]),
            FechaInicio = Convert.ToDateTime(dr["fecha_inicio"]),
            FechaFin = Convert.ToDateTime(dr["fecha_fin"]),
            IdEmpleado = Convert.ToInt32(dr["id_empleado"]),
            Empleado = Convert.ToString(dr["empleado"]) ?? "",
            Puesto = Convert.ToString(dr["puesto"]) ?? "",
            SueldoBase = Convert.ToDecimal(dr["sueldo_base"]),
            DescuentoTotal = Convert.ToDecimal(dr["descuento_total"]),
            MontoExtra = Convert.ToDecimal(dr["monto_extra"]),
            SalarioFinal = Convert.ToDecimal(dr["salario_final"]),
            Estado = Convert.ToString(dr["estado"]) ?? ""
        };
    }
}
