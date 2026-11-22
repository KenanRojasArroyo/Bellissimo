using Bellissimo.Entidades;
using MySql.Data.MySqlClient;

namespace Bellissimo.Datos;

public class AusenciaDatos
{
    private const string SelectBase = """
        SELECT a.id_ausencia, a.id_empleado,
               CONCAT(e.nombre, ' ', e.apellido_paterno) AS empleado,
               a.fecha, a.tipo, a.observaciones,
               fn_semana_cerrada(a.id_empleado, a.fecha) AS semana_cerrada
        FROM ausencia a
        INNER JOIN empleado e
            ON e.id_empleado = a.id_empleado
        """;

    // Filtros opcionales: null = sin filtro.
    public List<Ausencia> Listar(int? idEmpleado, DateTime? desde, DateTime? hasta)
    {
        List<Ausencia> lista = new();

        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectBase + """

            WHERE (@idEmpleado IS NULL OR a.id_empleado = @idEmpleado)
              AND (@desde IS NULL OR a.fecha >= @desde)
              AND (@hasta IS NULL OR a.fecha <= @hasta)
            ORDER BY a.fecha DESC, e.apellido_paterno, e.nombre;
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

    public Ausencia? BuscarPorId(int idAusencia)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = SelectBase + """

            WHERE a.id_ausencia = @idAusencia;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idAusencia", idAusencia);

        using MySqlDataReader dr = cmd.ExecuteReader();

        return dr.Read() ? Mapear(dr) : null;
    }

    // ¿El empleado ya tiene otra ausencia ese día? (uq_ausencia_dia)
    public bool ExisteEnFecha(int idEmpleado, DateTime fecha, int idAusenciaExcluir)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT COUNT(*)
            FROM ausencia
            WHERE id_empleado = @idEmpleado
              AND fecha = @fecha
              AND id_ausencia <> @idAusencia;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idEmpleado", idEmpleado);
        cmd.Parameters.AddWithValue("@fecha", fecha.Date);
        cmd.Parameters.AddWithValue("@idAusencia", idAusenciaExcluir);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public int Agregar(Ausencia ausencia)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            INSERT INTO ausencia
            (id_empleado, fecha, tipo, observaciones)
            VALUES
            (@idEmpleado, @fecha, @tipo, @observaciones);
            """;

        using MySqlCommand cmd = new(sql, cn);
        AgregarParametros(cmd, ausencia);

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

    public bool Modificar(Ausencia ausencia)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            UPDATE ausencia
            SET id_empleado = @idEmpleado,
                fecha = @fecha,
                tipo = @tipo,
                observaciones = @observaciones
            WHERE id_ausencia = @idAusencia;
            """;

        using MySqlCommand cmd = new(sql, cn);
        AgregarParametros(cmd, ausencia);
        cmd.Parameters.AddWithValue("@idAusencia", ausencia.IdAusencia);

        try
        {
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException ex) when (BaseDatosException.EsConocido(ex))
        {
            throw BaseDatosException.Desde(ex);
        }
    }

    public bool Eliminar(int idAusencia)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            DELETE FROM ausencia
            WHERE id_ausencia = @idAusencia;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idAusencia", idAusencia);

        try
        {
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException ex) when (BaseDatosException.EsConocido(ex))
        {
            throw BaseDatosException.Desde(ex);
        }
    }

    private static void AgregarParametros(MySqlCommand cmd, Ausencia ausencia)
    {
        cmd.Parameters.AddWithValue("@idEmpleado", ausencia.IdEmpleado);
        cmd.Parameters.AddWithValue("@fecha", ausencia.Fecha.Date);
        cmd.Parameters.AddWithValue("@tipo", ausencia.Tipo);
        cmd.Parameters.AddWithValue(
            "@observaciones",
            (object?)ausencia.Observaciones ?? DBNull.Value);
    }

    private static Ausencia Mapear(MySqlDataReader dr)
    {
        return new Ausencia
        {
            IdAusencia = Convert.ToInt32(dr["id_ausencia"]),
            IdEmpleado = Convert.ToInt32(dr["id_empleado"]),
            NombreEmpleado = Convert.ToString(dr["empleado"]),
            Fecha = Convert.ToDateTime(dr["fecha"]),
            Tipo = Convert.ToString(dr["tipo"]) ?? "",
            Observaciones =
                dr["observaciones"] == DBNull.Value
                    ? null
                    : Convert.ToString(dr["observaciones"]),
            SemanaCerrada = Convert.ToInt32(dr["semana_cerrada"]) == 1
        };
    }
}
