using System.Data;
using Bellissimo.Entidades;
using MySql.Data.MySqlClient;

namespace Bellissimo.Datos;

public class ChecadorDatos
{
    // sp_registrar_checada decide cuál de las cuatro checadas sigue.
    public ResultadoChecada Registrar(int idEmpleado, DateTime momento, string metodo)
    {
        using MySqlConnection cn = Conexion.Crear();
        cn.Open();

        using MySqlCommand cmd = new("sp_registrar_checada", cn)
        {
            CommandType = CommandType.StoredProcedure
        };

        cmd.Parameters.AddWithValue("p_id_empleado", idEmpleado);
        cmd.Parameters.AddWithValue("p_momento", momento);
        cmd.Parameters.AddWithValue("p_metodo", metodo);

        try
        {
            using MySqlDataReader dr = cmd.ExecuteReader();
            dr.Read();

            return new ResultadoChecada
            {
                IdEmpleado = idEmpleado,
                Checada = ConvertirChecada(Convert.ToString(dr["checada"]) ?? ""),
                Hora = Convert.ToDateTime(dr["hora"])
            };
        }
        catch (MySqlException ex) when (BaseDatosException.EsConocido(ex))
        {
            throw BaseDatosException.Desde(ex);
        }
    }

    private static TipoChecada ConvertirChecada(string checada) => checada switch
    {
        "ENTRADA" => TipoChecada.Entrada,
        "SALIDA_COMIDA" => TipoChecada.SalidaComida,
        "REGRESO_COMIDA" => TipoChecada.RegresoComida,
        "SALIDA" => TipoChecada.Salida,
        _ => throw new InvalidOperationException($"Checada desconocida: {checada}")
    };
}
