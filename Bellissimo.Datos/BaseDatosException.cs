using MySql.Data.MySqlClient;

namespace Bellissimo.Datos;

// Error de MySQL que Negocios sabe convertir en un mensaje para el usuario
// (ver Sql/CONTRATO_BD.md §5). Así Negocios no depende de MySql.Data.
public class BaseDatosException : Exception
{
    public const int ReglaNegocio = 1644;   // SIGNAL de un procedimiento o trigger
    public const int Duplicado = 1062;      // llave UNIQUE
    public const int TieneRelacionados = 1451;
    public const int ReferenciaInexistente = 1452;
    public const int RestriccionCheck = 3819;

    private static readonly int[] Conocidos =
    {
        ReglaNegocio, Duplicado, TieneRelacionados,
        ReferenciaInexistente, RestriccionCheck
    };

    public int Numero { get; }

    public BaseDatosException(int numero, string mensaje, Exception interna)
        : base(mensaje, interna)
    {
        Numero = numero;
    }

    public static bool EsConocido(MySqlException ex)
        => Conocidos.Contains(ex.Number);

    public static BaseDatosException Desde(MySqlException ex)
        => new(ex.Number, ex.Message, ex);
}
