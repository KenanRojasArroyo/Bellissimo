using Bellissimo.Datos;
using Bellissimo.Entidades;

namespace Bellissimo.Negocios;

// Pagos semanales. El cálculo lo hace sólo la base (sp_generar_pago):
// la aplicación genera, consulta, cierra y anota, pero nunca recalcula.
public class PagoNegocio
{
    private readonly PagoDatos _datos = new();
    private readonly SemanaDatos _semanaDatos = new();
    private readonly PersonalDatos _personalDatos = new();
    private readonly UsuarioDatos _usuarioDatos = new();

    // Reporte principal de pagos con filtros opcionales.
    public List<ResumenPago> ListarResumen(int? idSemana = null, int? idEmpleado = null, string? estado = null)
    {
        estado = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim();

        if (estado is not null && estado != EstadosPago.Borrador && estado != EstadosPago.Cerrado)
            throw new ArgumentException("Estado no válido: use Borrador o Cerrado.");

        return _datos.ListarResumen(
            idSemana is > 0 ? idSemana : null,
            idEmpleado is > 0 ? idEmpleado : null,
            estado);
    }

    public ResumenPago? BuscarResumen(int idPago)
    {
        if (idPago <= 0)
            return null;

        return _datos.BuscarResumenPorId(idPago);
    }

    // Genera (o regenera mientras esté en Borrador) el pago de un empleado.
    // Es la operación que escribe en pago_semanal y detalle_pago dentro de una transacción.
    public ResumenPago Generar(int idEmpleado, int idSemana, int? idUsuario = null)
    {
        ValidarSemana(idSemana);

        if (idEmpleado <= 0 || _personalDatos.BuscarPorId(idEmpleado) is null)
            throw new ArgumentException("Seleccione un empleado válido.");

        ValidarUsuario(idUsuario);

        ErroresBaseDatos.Ejecutar(() => _datos.Generar(idEmpleado, idSemana, idUsuario));

        return _datos.BuscarResumen(idEmpleado, idSemana)
               ?? throw new InvalidOperationException("El pago no quedó registrado.");
    }

    // Genera el pago de todos los empleados activos que ya habían ingresado en esa semana.
    // Los que no se pueden generar (por ejemplo, pago ya cerrado) se reportan en Omitidos.
    public ResultadoGeneracionPagos GenerarSemana(int idSemana, int? idUsuario = null)
    {
        Semana semana = ValidarSemana(idSemana);
        ValidarUsuario(idUsuario);

        ResultadoGeneracionPagos resultado = new();

        IEnumerable<Personal> empleados = _personalDatos.Listar()
            .Where(e => e.Activo && e.FechaRegistro.Date <= semana.FechaFin.Date);

        foreach (Personal empleado in empleados)
        {
            try
            {
                ErroresBaseDatos.Ejecutar(() => _datos.Generar(empleado.IdPersonal, idSemana, idUsuario));
                resultado.Generados++;
            }
            catch (ReglaNegocioException ex)
            {
                resultado.Omitidos.Add($"{empleado.Nombre} {empleado.ApellidoPaterno}: {ex.Message}");
            }
        }

        return resultado;
    }

    // Ticket tal como quedó guardado; null si el pago todavía no se generó.
    public Ticket? ObtenerTicket(int idEmpleado, int idSemana)
    {
        if (idEmpleado <= 0 || idSemana <= 0)
            return null;

        ResumenPago? resumen = _datos.BuscarResumen(idEmpleado, idSemana);

        if (resumen is null)
            return null;

        Ticket ticket = ErroresBaseDatos.Ejecutar(() => _datos.ObtenerTicket(idEmpleado, idSemana));
        ticket.IdPago = resumen.IdPago;
        return ticket;
    }

    public Ticket? ObtenerTicketPorPago(int idPago)
    {
        ResumenPago? resumen = BuscarResumen(idPago);

        return resumen is null
            ? null
            : ObtenerTicket(resumen.IdEmpleado, resumen.IdSemana);
    }

    // Cierra el pago: a partir de aquí no cambian checadas, ausencias ni montos.
    // Antes exige que no queden días sin checada ni ausencia (V10).
    public bool Cerrar(int idPago)
    {
        if (idPago <= 0)
            throw new ArgumentException("Pago no válido.");

        ResumenPago? pago = _datos.BuscarResumenPorId(idPago);

        if (pago is null)
            return false;

        if (pago.Cerrado)
            throw new ReglaNegocioException("El pago ya estaba cerrado.");

        List<DiaSinChecada> pendientes = _datos.ListarDiasSinChecada(pago.IdSemana, pago.IdEmpleado);

        if (pendientes.Count > 0)
        {
            string dias = string.Join(", ", pendientes.Select(d => d.Fecha.ToString("dd/MM/yyyy")));

            throw new ReglaNegocioException(
                $"Antes de cerrar, registre la checada o la ausencia de: {dias}. " +
                "Después vuelva a generar el pago.");
        }

        ErroresBaseDatos.Ejecutar(() => _datos.Cerrar(idPago));
        return true;
    }

    // Nota del encargado; se permite aunque el pago esté cerrado.
    public bool AgregarObservacion(int idPago, string? texto)
    {
        if (idPago <= 0)
            throw new ArgumentException("Pago no válido.");

        texto = string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();

        if (texto is { Length: > 255 })
            throw new ArgumentException("La observación no puede exceder los 255 caracteres.");

        if (_datos.BuscarResumenPorId(idPago) is null)
            return false;

        ErroresBaseDatos.Ejecutar(() => _datos.AgregarObservacion(idPago, texto));
        return true;
    }

    // Aviso antes de generar o cerrar (v_dias_sin_checada).
    public List<DiaSinChecada> DiasSinChecada(int idSemana, int? idEmpleado = null)
    {
        if (idSemana <= 0)
            return new List<DiaSinChecada>();

        return _datos.ListarDiasSinChecada(idSemana, idEmpleado is > 0 ? idEmpleado : null);
    }

    private Semana ValidarSemana(int idSemana)
    {
        Semana? semana = idSemana > 0 ? _semanaDatos.BuscarPorId(idSemana) : null;

        return semana ?? throw new ArgumentException("Seleccione una semana válida.");
    }

    private void ValidarUsuario(int? idUsuario)
    {
        if (idUsuario is not null && !_usuarioDatos.ExisteActivo(idUsuario.Value))
            throw new ArgumentException("El usuario indicado no existe o está inactivo.");
    }
}
