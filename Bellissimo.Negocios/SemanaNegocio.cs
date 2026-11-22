using Bellissimo.Datos;
using Bellissimo.Entidades;

namespace Bellissimo.Negocios;

// Semanas de pago: de lunes a sábado.
public class SemanaNegocio
{
    private readonly SemanaDatos _datos = new();

    public List<Semana> Listar()
        => _datos.Listar();

    public Semana? BuscarPorId(int idSemana)
    {
        if (idSemana <= 0)
            return null;

        return _datos.BuscarPorId(idSemana);
    }

    // Registra la semana que empieza en fechaInicio (debe ser lunes).
    public int Agregar(DateTime fechaInicio)
    {
        fechaInicio = fechaInicio.Date;

        if (fechaInicio.DayOfWeek != DayOfWeek.Monday)
            throw new ArgumentException("La semana debe iniciar en lunes.");

        if (_datos.ExisteInicio(fechaInicio))
            throw new ArgumentException("Esa semana ya está registrada.");

        Semana semana = new()
        {
            FechaInicio = fechaInicio,
            FechaFin = fechaInicio.AddDays(5)
        };

        return ErroresBaseDatos.Ejecutar(() => _datos.Agregar(semana));
    }

    // Lunes de la semana a la que pertenece una fecha.
    public static DateTime LunesDe(DateTime fecha)
    {
        int diasDesdeLunes = ((int)fecha.DayOfWeek + 6) % 7;
        return fecha.Date.AddDays(-diasDesdeLunes);
    }
}
