using Bellissimo.Datos;
using Bellissimo.Entidades;

namespace Bellissimo.Negocios;

public class AusenciaNegocio
{
    private readonly AusenciaDatos _datos = new();
    private readonly PersonalDatos _personalDatos = new();
    private readonly RegistroAsistenciaDatos _registroDatos = new();

    public static IReadOnlyList<string> Tipos => TiposAusencia.Todos;

    public List<Ausencia> Listar(int? idEmpleado = null, DateTime? desde = null, DateTime? hasta = null)
    {
        if (desde is not null && hasta is not null && desde.Value.Date > hasta.Value.Date)
            throw new ArgumentException("La fecha inicial no puede ser posterior a la final.");

        return _datos.Listar(idEmpleado is > 0 ? idEmpleado : null, desde, hasta);
    }

    public Ausencia? BuscarPorId(int idAusencia)
    {
        if (idAusencia <= 0)
            return null;

        return _datos.BuscarPorId(idAusencia);
    }

    public int Agregar(Ausencia ausencia)
    {
        Validar(ausencia, 0);
        return ErroresBaseDatos.Ejecutar(() => _datos.Agregar(ausencia));
    }

    public bool Modificar(Ausencia ausencia)
    {
        if (ausencia.IdAusencia <= 0)
            throw new ArgumentException("Ausencia no válida.");

        Ausencia? actual = _datos.BuscarPorId(ausencia.IdAusencia);

        if (actual is null)
            return false;

        if (actual.SemanaCerrada)
            throw new ReglaNegocioException(
                "No se puede modificar: el pago de esa semana ya está cerrado.");

        Validar(ausencia, ausencia.IdAusencia);
        return ErroresBaseDatos.Ejecutar(() => _datos.Modificar(ausencia));
    }

    public bool Eliminar(int idAusencia)
    {
        if (idAusencia <= 0)
            throw new ArgumentException("Ausencia no válida.");

        Ausencia? actual = _datos.BuscarPorId(idAusencia);

        if (actual is null)
            return false;

        if (actual.SemanaCerrada)
            throw new ReglaNegocioException(
                "No se puede eliminar: el pago de esa semana ya está cerrado.");

        return ErroresBaseDatos.Ejecutar(() => _datos.Eliminar(idAusencia));
    }

    private void Validar(Ausencia ausencia, int idAusenciaExcluir)
    {
        ausencia.Tipo = ausencia.Tipo?.Trim() ?? "";
        ausencia.Fecha = ausencia.Fecha.Date;
        ausencia.Observaciones =
            string.IsNullOrWhiteSpace(ausencia.Observaciones)
                ? null
                : ausencia.Observaciones.Trim();

        Personal? empleado =
            ausencia.IdEmpleado > 0 ? _personalDatos.BuscarPorId(ausencia.IdEmpleado) : null;

        if (empleado is null)
            throw new ArgumentException("Seleccione un empleado válido.");

        if (!TiposAusencia.Todos.Contains(ausencia.Tipo))
            throw new ArgumentException(
                "Seleccione el tipo de ausencia: Justificada, Injustificada o Vacaciones.");

        if (ausencia.Fecha.DayOfWeek == DayOfWeek.Sunday)
            throw new ArgumentException("El domingo no es día laboral.");

        if (ausencia.Fecha < empleado.FechaRegistro.Date)
            throw new ArgumentException("La fecha es anterior al ingreso del empleado.");

        if (ausencia.Observaciones is { Length: > 200 })
            throw new ArgumentException(
                "Las observaciones no pueden exceder los 200 caracteres.");

        if (_datos.ExisteEnFecha(ausencia.IdEmpleado, ausencia.Fecha, idAusenciaExcluir))
            throw new ArgumentException("El empleado ya tiene una ausencia registrada ese día.");

        // RN17: un día es checada o ausencia, nunca las dos.
        if (_registroDatos.BuscarPorEmpleadoYFecha(ausencia.IdEmpleado, ausencia.Fecha) is not null)
            throw new ArgumentException(
                "El empleado ya tiene checadas ese día; no se puede registrar una ausencia.");
    }
}
