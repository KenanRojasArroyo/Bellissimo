using Bellissimo.Entidades;
using MySql.Data.MySqlClient;

namespace Bellissimo.Datos;

// app_floreria tiene SELECT e INSERT sobre semana.
public class SemanaDatos
{
    public List<Semana> Listar()
    {
        List<Semana> lista = new();

        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT id_semana, fecha_inicio, fecha_fin
            FROM semana
            ORDER BY fecha_inicio DESC;
            """;

        using MySqlCommand cmd = new(sql, cn);
        using MySqlDataReader dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            lista.Add(Mapear(dr));
        }

        return lista;
    }

    public Semana? BuscarPorId(int idSemana)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT id_semana, fecha_inicio, fecha_fin
            FROM semana
            WHERE id_semana = @idSemana;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idSemana", idSemana);

        using MySqlDataReader dr = cmd.ExecuteReader();

        return dr.Read() ? Mapear(dr) : null;
    }

    public bool ExisteInicio(DateTime fechaInicio)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT COUNT(*)
            FROM semana
            WHERE fecha_inicio = @fechaInicio;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@fechaInicio", fechaInicio.Date);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public int Agregar(Semana semana)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            INSERT INTO semana (fecha_inicio, fecha_fin)
            VALUES (@fechaInicio, @fechaFin);
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@fechaInicio", semana.FechaInicio.Date);
        cmd.Parameters.AddWithValue("@fechaFin", semana.FechaFin.Date);

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

    private static Semana Mapear(MySqlDataReader dr)
    {
        return new Semana
        {
            IdSemana = Convert.ToInt32(dr["id_semana"]),
            FechaInicio = Convert.ToDateTime(dr["fecha_inicio"]),
            FechaFin = Convert.ToDateTime(dr["fecha_fin"])
        };
    }
}
