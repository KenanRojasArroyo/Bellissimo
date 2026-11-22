using Bellissimo.Entidades;
using MySql.Data.MySqlClient;

namespace Bellissimo.Datos;

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
            lista.Add(new Puesto
            {
                IdPuesto = Convert.ToInt32(dr["id_puesto"]),
                Nombre = Convert.ToString(dr["nombre"]) ?? ""
            });
        }

        return lista;
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
}
