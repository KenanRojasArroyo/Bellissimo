using Bellissimo.Datos;
using Bellissimo.Entidades;

namespace Bellissimo.Negocios;

// Consulta, corrección manual y reporte de checadas (registro_asistencia).
// Las correcciones quedan con metodo = 'Manual' y el usuario que corrigió;
// la auditoría la escribe el trigger de la base, no la aplicación.
public class AsistenciaNegocio
{
    private readonly RegistroAsistenciaDatos _datos = new();
    private readonly PersonalDatos _personalDatos = new();
    private readonly AusenciaDatos _ausenciaDatos = new();
    private readonly UsuarioDatos _usuarioDatos = new();

    public List<RegistroAsistencia> Listar(int? idEmpleado = null, DateTime? desde = null, DateTime? hasta = null)
    {
        ValidarRango(desde, hasta);
        return _datos.Listar(idEmpleado is > 0 ? idEmpleado : null, desde, hasta);
    }

    public RegistroAsistencia? BuscarPorId(int idRegistro)
    {
        if (idRegistro <= 0)
            return null;

        return _datos.BuscarPorId(idRegistro);
    }

    // Reporte de asistencia diaria (vista v_asistencia_diaria).
    public List<AsistenciaDiaria> ReporteDiario(DateTime desde, DateTime hasta, int? idEmpleado = null)
    {
        ValidarRango(desde, hasta);
        return _datos.ReporteDiario(desde, hasta, idEmpleado is > 0 ? idEmpleado : null);
    }

    // Captura manual de un día que el empleado no checó (lo olvidó, falló el equipo...).
    public int Agregar(RegistroAsistencia registro)
    {
        Validar(registro, 0);
        return ErroresBaseDatos.Ejecutar(() => _datos.Agregar(registro));
    }

    public bool Modificar(RegistroAsistencia registro)
    {
        if (registro.IdRegistro <= 0)
            throw new ArgumentException("Registro no válido.");

        RegistroAsistencia? actual = _datos.BuscarPorId(registro.IdRegistro);

        if (actual is null)
            return false;

        if (actual.SemanaCerrada)
            throw new ReglaNegocioException(
                "No se puede corregir: el pago de esa semana ya está cerrado.");

        Validar(registro, registro.IdRegistro);
        return ErroresBaseDatos.Ejecutar(() => _datos.Modificar(registro));
    }

    public bool Eliminar(int idRegistro)
    {
        if (idRegistro <= 0)
            throw new ArgumentException("Registro no válido.");

        RegistroAsistencia? actual = _datos.BuscarPorId(idRegistro);

        if (actual is null)
            return false;

        if (actual.SemanaCerrada)
            throw new ReglaNegocioException(
                "No se puede eliminar: el pago de esa semana ya está cerrado.");

        return ErroresBaseDatos.Ejecutar(() => _datos.Eliminar(idRegistro));
    }

    private static void ValidarRango(DateTime? desde, DateTime? hasta)
    {
        if (desde is not null && hasta is not null && desde.Value.Date > hasta.Value.Date)
            throw new ArgumentException("La fecha inicial no puede ser posterior a la final.");
    }

    private void Validar(RegistroAsistencia registro, int idRegistroExcluir)
    {
        registro.Fecha = registro.Fecha.Date;
        registro.Metodo = "Manual";

        Personal? empleado =
            registro.IdEmpleado > 0 ? _personalDatos.BuscarPorId(registro.IdEmpleado) : null;

        if (empleado is null)
            throw new ArgumentException("Seleccione un empleado válido.");

        if (registro.ModificadoPor is not > 0 || !_usuarioDatos.ExisteActivo(registro.ModificadoPor.Value))
            throw new ArgumentException("Seleccione quién hace la corrección.");

        if (registro.Fecha.DayOfWeek == DayOfWeek.Sunday)
            throw new ArgumentException("El domingo no es día laboral.");

        if (registro.Fecha > DateTime.Today)
            throw new ArgumentException("No se pueden capturar checadas de días futuros.");

        if (registro.Fecha < empleado.FechaRegistro.Date)
            throw new ArgumentException("La fecha es anterior al ingreso del empleado.");

        if (registro.Entrada is null)
            throw new ArgumentException("La hora de entrada es obligatoria.");

        if (registro.RegresoComida is not null && registro.SalidaComida is null)
            throw new ArgumentException("No puede haber regreso de comer sin salida a comer.");

        DateTime?[] horas =
        {
            registro.Entrada, registro.SalidaComida, registro.RegresoComida, registro.Salida
        };

        if (horas.Any(h => h is not null && h.Value.Date != registro.Fecha))
            throw new ArgumentException("Todas las horas deben ser del mismo día del registro.");

        // Orden: entrada <= salida a comer <= regreso <= salida (las que existan).
        DateTime? anterior = null;

        foreach (DateTime? hora in horas)
        {
            if (hora is null)
                continue;

            if (anterior is not null && hora < anterior)
                throw new ArgumentException(
                    "Las horas deben ir en orden: entrada, salida a comer, regreso y salida.");

            anterior = hora;
        }

        if (_datos.ExisteEnFecha(registro.IdEmpleado, registro.Fecha, idRegistroExcluir))
            throw new ArgumentException("El empleado ya tiene un registro de checadas ese día.");

        // RN17: un día es checada o ausencia, nunca las dos.
        if (_ausenciaDatos.ExisteEnFecha(registro.IdEmpleado, registro.Fecha, 0))
            throw new ArgumentException(
                "El empleado tiene una ausencia registrada ese día; elimínela antes de capturar checadas.");
    }
}
