using Bellissimo.Entidades;
using MySql.Data.MySqlClient;

namespace Bellissimo.Datos;

// Checadas por día (registro_asistencia). Las checadas normales entran por
// sp_registrar_checada (ChecadorDatos); aquí están la consulta y las
// correcciones manuales del encargado (regla 5 del contrato). La auditoría
// la escribe el trigger trg_registro_au_auditoria, no la aplicación.
public class RegistroAsistenciaDatos
{
    private const string SelectBase = """
        SELECT r.id_registro, r.id_empleado,
               CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado,
               r.fecha, r.entrada, r.salida_comida, r.regreso_comida,
               r.salida, r.metodo, r.modificado_por,
               u.nombre_completo AS nombre_modificado_por,
               fn_semana_cerrada(r.id_empleado, r.fecha) AS semana_cerrada
        FROM registro_asistencia r
        INNER JOIN empleado e
            ON e.id_empleado = r.id_empleado
        LEFT JOIN usuario u
            ON u.id_usuario = r.modificado_por
        """;

    public List<RegistroAsistencia> Listar(int? idEmpleado, DateTime? desde, DateTime? hasta)
    {
        List<RegistroAsistencia> lista = new();

        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectBase + """

            WHERE (@idEmpleado IS NULL OR r.id_empleado = @idEmpleado)
              AND (@desde IS NULL OR r.fecha >= @desde)
              AND (@hasta IS NULL OR r.fecha <= @hasta)
            ORDER BY r.fecha DESC, e.apellido_paterno, e.nombre;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idEmpleado", (object?)idEmpleado ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@desde", (object?)desde?.Date ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@hasta", (object?)hasta?.Date ?? DBNull.Value);

        using MySqlDataReader dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            lista.Add(Mapear(dr));
        }

        return lista;
    }

    public RegistroAsistencia? BuscarPorId(int idRegistro)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectBase + """

            WHERE r.id_registro = @idRegistro;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idRegistro", idRegistro);

        using MySqlDataReader dr = cmd.ExecuteReader();

        return dr.Read() ? Mapear(dr) : null;
    }

    public RegistroAsistencia? BuscarPorEmpleadoYFecha(int idEmpleado, DateTime fecha)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectBase + """

            WHERE r.id_empleado = @idEmpleado
              AND r.fecha = @fecha;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idEmpleado", idEmpleado);
        cmd.Parameters.AddWithValue("@fecha", fecha.Date);

        using MySqlDataReader dr = cmd.ExecuteReader();

        return dr.Read() ? Mapear(dr) : null;
    }

    // ¿El empleado ya tiene otro registro ese día? (uq_registro_dia)
    public bool ExisteEnFecha(int idEmpleado, DateTime fecha, int idRegistroExcluir)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT COUNT(*)
            FROM registro_asistencia
            WHERE id_empleado = @idEmpleado
              AND fecha = @fecha
              AND id_registro <> @idRegistro;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idEmpleado", idEmpleado);
        cmd.Parameters.AddWithValue("@fecha", fecha.Date);
        cmd.Parameters.AddWithValue("@idRegistro", idRegistroExcluir);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public int Agregar(RegistroAsistencia registro)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            INSERT INTO registro_asistencia
            (id_empleado, fecha, entrada, salida_comida,
             regreso_comida, salida, metodo, modificado_por)
            VALUES
            (@idEmpleado, @fecha, @entrada, @salidaComida,
             @regresoComida, @salida, 'Manual', @modificadoPor);
            """;

        using MySqlCommand cmd = new(sql, cn);
        AgregarParametros(cmd, registro);

        try
        {
            cmd.ExecuteNonQuery();
        }
        catch (MySqlException ex) when (BaseDatosException.EsConocido(ex))
        {
            throw BaseDatosException.Desde(ex);
        }

        return Convert.ToInt32(cmd.LastInsertedId);
    }

    public bool Modificar(RegistroAsistencia registro)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            UPDATE registro_asistencia
            SET id_empleado = @idEmpleado,
                fecha = @fecha,
                entrada = @entrada,
                salida_comida = @salidaComida,
                regreso_comida = @regresoComida,
                salida = @salida,
                metodo = 'Manual',
                modificado_por = @modificadoPor
            WHERE id_registro = @idRegistro;
            """;

        using MySqlCommand cmd = new(sql, cn);
        AgregarParametros(cmd, registro);
        cmd.Parameters.AddWithValue("@idRegistro", registro.IdRegistro);

        try
        {
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException ex) when (BaseDatosException.EsConocido(ex))
        {
            throw BaseDatosException.Desde(ex);
        }
    }

    public bool Eliminar(int idRegistro)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            DELETE FROM registro_asistencia
            WHERE id_registro = @idRegistro;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idRegistro", idRegistro);

        try
        {
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException ex) when (BaseDatosException.EsConocido(ex))
        {
            throw BaseDatosException.Desde(ex);
        }
    }

    // Reporte de asistencia (vista v_asistencia_diaria).
    public List<AsistenciaDiaria> ReporteDiario(DateTime desde, DateTime hasta, int? idEmpleado)
    {
        List<AsistenciaDiaria> lista = new();

        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT id_registro, id_empleado, empleado, fecha, entrada, salida,
                   horas_presentes, min_comida, min_comida_extra, min_retardo,
                   min_salida_temprana, min_extra, metodo
            FROM v_asistencia_diaria
            WHERE fecha BETWEEN @desde AND @hasta
              AND (@idEmpleado IS NULL OR id_empleado = @idEmpleado)
            ORDER BY fecha, empleado;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@desde", desde.Date);
        cmd.Parameters.AddWithValue("@hasta", hasta.Date);
        cmd.Parameters.AddWithValue("@idEmpleado", (object?)idEmpleado ?? DBNull.Value);

        using MySqlDataReader dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            lista.Add(new AsistenciaDiaria
            {
                IdRegistro = Convert.ToInt32(dr["id_registro"]),
                IdEmpleado = Convert.ToInt32(dr["id_empleado"]),
                Empleado = Convert.ToString(dr["empleado"]) ?? "",
                Fecha = Convert.ToDateTime(dr["fecha"]),
                Entrada = dr["entrada"] == DBNull.Value ? null : (TimeSpan)dr["entrada"],
                Salida = dr["salida"] == DBNull.Value ? null : (TimeSpan)dr["salida"],
                HorasPresentes = Convert.ToDecimal(dr["horas_presentes"]),
                MinComida = Convert.ToInt32(dr["min_comida"]),
                MinComidaExtra = Convert.ToInt32(dr["min_comida_extra"]),
                MinRetardo = Convert.ToInt32(dr["min_retardo"]),
                MinSalidaTemprana = Convert.ToInt32(dr["min_salida_temprana"]),
                MinExtra = Convert.ToInt32(dr["min_extra"]),
                Metodo = Convert.ToString(dr["metodo"]) ?? ""
            });
        }

        return lista;
    }

    private static void AgregarParametros(MySqlCommand cmd, RegistroAsistencia registro)
    {
        cmd.Parameters.AddWithValue("@idEmpleado", registro.IdEmpleado);
        cmd.Parameters.AddWithValue("@fecha", registro.Fecha.Date);
        cmd.Parameters.AddWithValue("@entrada", (object?)registro.Entrada ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@salidaComida", (object?)registro.SalidaComida ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@regresoComida", (object?)registro.RegresoComida ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@salida", (object?)registro.Salida ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@modificadoPor", (object?)registro.ModificadoPor ?? DBNull.Value);
    }

    private static DateTime? Fecha(MySqlDataReader dr, string columna)
        => dr[columna] == DBNull.Value ? null : Convert.ToDateTime(dr[columna]);

    private static RegistroAsistencia Mapear(MySqlDataReader dr)
    {
        return new RegistroAsistencia
        {
            IdRegistro = Convert.ToInt32(dr["id_registro"]),
            IdEmpleado = Convert.ToInt32(dr["id_empleado"]),
            NombreEmpleado = Convert.ToString(dr["empleado"]),
            Fecha = Convert.ToDateTime(dr["fecha"]),
            Entrada = Fecha(dr, "entrada"),
            SalidaComida = Fecha(dr, "salida_comida"),
            RegresoComida = Fecha(dr, "regreso_comida"),
            Salida = Fecha(dr, "salida"),
            Metodo = Convert.ToString(dr["metodo"]) ?? "",
            ModificadoPor =
                dr["modificado_por"] == DBNull.Value
                    ? null
                    : Convert.ToInt32(dr["modificado_por"]),
            NombreModificadoPor =
                dr["nombre_modificado_por"] == DBNull.Value
                    ? null
                    : Convert.ToString(dr["nombre_modificado_por"]),
            SemanaCerrada = Convert.ToInt32(dr["semana_cerrada"]) == 1
        };
    }
}
