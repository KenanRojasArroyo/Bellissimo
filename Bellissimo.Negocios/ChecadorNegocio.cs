using Bellissimo.Datos;
using Bellissimo.Entidades;

namespace Bellissimo.Negocios;

// Checador: el empleado escribe su número y su clave personal.
// La clave se compara contra el hash (regla 6 del contrato) y la checada
// se registra con la hora del servidor mediante sp_registrar_checada.
public class ChecadorNegocio
{
    private const string MensajeCredencialIncorrecta = "Número de empleado o clave incorrectos.";

    private readonly ChecadorDatos _datos = new();
    private readonly PersonalDatos _personalDatos = new();
    private readonly RegistroAsistenciaDatos _registroDatos = new();
    private readonly Func<DateTime> _reloj;

    public ChecadorNegocio()
        : this(() => DateTime.Now)
    {
    }

    // Sólo para pruebas: permite fijar la hora "actual".
    internal ChecadorNegocio(Func<DateTime> reloj)
    {
        _reloj = reloj;
    }

    // Siguiente checada de hoy para el empleado; null si ya tiene las cuatro.
    public TipoChecada? SiguienteChecada(int idEmpleado)
    {
        if (idEmpleado <= 0)
            return TipoChecada.Entrada;

        return Siguiente(_registroDatos.BuscarPorEmpleadoYFecha(idEmpleado, _reloj().Date));
    }

    // esperada: la checada de la pantalla (por ejemplo, la página "Entrada").
    // Si es null, se registra la que siga.
    public ResultadoChecada Registrar(int idEmpleado, string clave, TipoChecada? esperada = null)
    {
        if (idEmpleado <= 0)
            throw new ArgumentException("Capture su número de empleado.");

        if (string.IsNullOrWhiteSpace(clave))
            throw new ArgumentException("Capture su clave personal.");

        Personal? empleado = _personalDatos.BuscarPorId(idEmpleado);

        if (empleado is null || !empleado.Activo)
            throw new ReglaNegocioException(MensajeCredencialIncorrecta);

        string? hash = _personalDatos.ObtenerClaveHash(idEmpleado);

        if (!ClaveHasher.EsHashValido(hash))
            throw new ReglaNegocioException(
                "El empleado no tiene clave asignada. Pida al encargado que la registre en Personal.");

        if (!ClaveHasher.Verificar(clave.Trim(), hash))
            throw new ReglaNegocioException(MensajeCredencialIncorrecta);

        DateTime ahora = SinFraccion(_reloj());

        if (ahora.DayOfWeek == DayOfWeek.Sunday)
            throw new ReglaNegocioException("No se checa los domingos: no hay horario.");

        if (esperada is not null)
        {
            TipoChecada? siguiente =
                Siguiente(_registroDatos.BuscarPorEmpleadoYFecha(idEmpleado, ahora.Date));

            if (siguiente is null)
                throw new ReglaNegocioException("Ya registró las cuatro checadas de hoy.");

            if (siguiente != esperada)
                throw new ReglaNegocioException(
                    $"Su siguiente checada de hoy es «{siguiente.Value.Descripcion()}», " +
                    $"no «{esperada.Value.Descripcion()}».");
        }

        ResultadoChecada resultado =
            ErroresBaseDatos.Ejecutar(() => _datos.Registrar(idEmpleado, ahora, "Clave"));

        resultado.NombreEmpleado = $"{empleado.Nombre} {empleado.ApellidoPaterno}";
        return resultado;
    }

    private static TipoChecada? Siguiente(RegistroAsistencia? registro)
    {
        if (registro?.Entrada is null) return TipoChecada.Entrada;
        if (registro.SalidaComida is null) return TipoChecada.SalidaComida;
        if (registro.RegresoComida is null) return TipoChecada.RegresoComida;
        if (registro.Salida is null) return TipoChecada.Salida;
        return null;
    }

    // DATETIME de MySQL guarda segundos; se quita la fracción para no redondear.
    private static DateTime SinFraccion(DateTime valor)
        => new(valor.Ticks - valor.Ticks % TimeSpan.TicksPerSecond, valor.Kind);
}
