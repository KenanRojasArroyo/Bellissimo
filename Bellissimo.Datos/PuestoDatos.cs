using Bellissimo.Entidades;
using MySql.Data.MySqlClient;

namespace Bellissimo.Datos;

// Catálogo de puestos. app_floreria tiene SELECT, INSERT y UPDATE, pero no
// DELETE: por diseño un puesto no se borra (RN15).
public class PuestoDatos
{
    public List<Puesto> Listar()
    {
        List<Puesto> lista = new();

        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT id_puesto, nombre
            FROM puesto
            ORDER BY nombre;
            """;

        using MySqlCommand cmd = new(sql, cn);
        using MySqlDataReader dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            lista.Add(Mapear(dr));
        }

        return lista;
    }

    public Puesto? BuscarPorId(int idPuesto)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT id_puesto, nombre
            FROM puesto
            WHERE id_puesto = @idPuesto;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idPuesto", idPuesto);

        using MySqlDataReader dr = cmd.ExecuteReader();

        return dr.Read() ? Mapear(dr) : null;
    }

    public bool Existe(int idPuesto)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT COUNT(*)
            FROM puesto
            WHERE id_puesto = @idPuesto;
            """;

        using MySqlCommand cmd = new(sql, cn);

        cmd.Parameters.AddWithValue(
            "@idPuesto",
            idPuesto);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    // ¿Otro puesto ya usa ese nombre? (nombre es UNIQUE)
    public bool ExisteNombre(string nombre, int idPuestoExcluir)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT COUNT(*)
            FROM puesto
            WHERE nombre = @nombre
              AND id_puesto <> @idPuesto;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@idPuesto", idPuestoExcluir);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }

    public int Agregar(Puesto puesto)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            INSERT INTO puesto (nombre)
            VALUES (@nombre);
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@nombre", puesto.Nombre);

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

    public bool Modificar(Puesto puesto)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            UPDATE puesto
            SET nombre = @nombre
            WHERE id_puesto = @idPuesto;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@nombre", puesto.Nombre);
        cmd.Parameters.AddWithValue("@idPuesto", puesto.IdPuesto);

        try
        {
            return cmd.ExecuteNonQuery() > 0;
        }
        catch (MySqlException ex) when (BaseDatosException.EsConocido(ex))
        {
            throw BaseDatosException.Desde(ex);
        }
    }

    private static Puesto Mapear(MySqlDataReader dr)
    {
        return new Puesto
        {
            IdPuesto = Convert.ToInt32(dr["id_puesto"]),
            Nombre = Convert.ToString(dr["nombre"]) ?? ""
        };
    }
}
