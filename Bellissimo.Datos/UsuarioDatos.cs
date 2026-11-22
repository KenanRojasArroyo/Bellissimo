using Bellissimo.Entidades;
using MySql.Data.MySqlClient;

namespace Bellissimo.Datos;

// Sólo lectura: la contraseña nunca se lee desde aquí.
public class UsuarioDatos
{
    public List<Usuario> ListarActivos()
    {
        List<Usuario> lista = new();

        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT id_usuario, nombre_completo, nombre_usuario, rol, activo
            FROM usuario
            WHERE activo = 1
            ORDER BY nombre_completo;
            """;

        using MySqlCommand cmd = new(sql, cn);
        using MySqlDataReader dr = cmd.ExecuteReader();

        while (dr.Read())
        {
            lista.Add(new Usuario
            {
                IdUsuario = Convert.ToInt32(dr["id_usuario"]),
                NombreCompleto = Convert.ToString(dr["nombre_completo"]) ?? "",
                NombreUsuario = Convert.ToString(dr["nombre_usuario"]) ?? "",
                Rol = Convert.ToString(dr["rol"]) ?? "",
                Activo = Convert.ToBoolean(dr["activo"])
            });
        }

        return lista;
    }

    public bool ExisteActivo(int idUsuario)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        const string sql = """
            SELECT COUNT(*)
            FROM usuario
            WHERE id_usuario = @idUsuario
              AND activo = 1;
            """;

        using MySqlCommand cmd = new(sql, cn);
        cmd.Parameters.AddWithValue("@idUsuario", idUsuario);

        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
    }
}
