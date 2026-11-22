namespace Bellissimo.Entidades;

// Orden fijo de las cuatro checadas del día.
public enum TipoChecada
{
    Entrada = 1,
    SalidaComida = 2,
    RegresoComida = 3,
    Salida = 4
}

public static class TipoChecadaExtensiones
{
    public static string Descripcion(this TipoChecada tipo) => tipo switch
    {
        TipoChecada.Entrada => "Entrada",
        TipoChecada.SalidaComida => "Salida a comer",
        TipoChecada.RegresoComida => "Regreso de comer",
        TipoChecada.Salida => "Salida",
        _ => tipo.ToString()
    };
}

public class ResultadoChecada
{
    public int IdEmpleado { get; set; }
    public string NombreEmpleado { get; set; } = string.Empty;
    public TipoChecada Checada { get; set; }
    public DateTime Hora { get; set; }
}
