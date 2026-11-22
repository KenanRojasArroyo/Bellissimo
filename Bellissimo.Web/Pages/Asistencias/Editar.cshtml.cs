using System.ComponentModel.DataAnnotations;
using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bellissimo.Web.Pages.Asistencias;

public class EditarModel : PageModel
{
    [BindProperty]
    public int IdRegistro { get; set; }

    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un empleado.")]
    public int IdEmpleado { get; set; }

    public string? NombreEmpleado { get; private set; }

    [BindProperty]
    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "La hora de entrada es obligatoria.")]
    public TimeSpan? HoraEntrada { get; set; }

    [BindProperty]
    public TimeSpan? HoraSalidaComida { get; set; }

    [BindProperty]
    public TimeSpan? HoraRegresoComida { get; set; }

    [BindProperty]
    public TimeSpan? HoraSalida { get; set; }

    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione quién hace la corrección.")]
    public int ModificadoPor { get; set; }

    public bool SemanaCerrada { get; private set; }

    public List<SelectListItem> ListaEmpleados { get; private set; } = new();

    public List<SelectListItem> ListaUsuarios { get; private set; } = new();

    public string? Error { get; private set; }

    public IActionResult OnGet(int id)
    {
        try
        {
            CargarCatalogos();
            AsistenciaNegocio negocio = new();
            RegistroAsistencia? reg = negocio.BuscarPorId(id);

            if (reg is null)
                return NotFound();

            IdRegistro = reg.IdRegistro;
            IdEmpleado = reg.IdEmpleado;
            NombreEmpleado = reg.NombreEmpleado;
            Fecha = reg.Fecha.Date;
            HoraEntrada = reg.Entrada?.TimeOfDay;
            HoraSalidaComida = reg.SalidaComida?.TimeOfDay;
            HoraRegresoComida = reg.RegresoComida?.TimeOfDay;
            HoraSalida = reg.Salida?.TimeOfDay;
            ModificadoPor = reg.ModificadoPor ?? 0;
            SemanaCerrada = reg.SemanaCerrada;

            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar el registro de asistencia. Verifique la conexión con MySQL.";
            return Page();
        }
    }

    public IActionResult OnPost()
    {
        try
        {
            CargarCatalogos();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ModelState.AddModelError(string.Empty, "No fue posible cargar los catálogos.");
            return Page();
        }

        if (!ModelState.IsValid)
        {
            RecargarEstado(IdRegistro);
            return Page();
        }

        try
        {
            DateTime dia = Fecha.Date;

            RegistroAsistencia registro = new()
            {
                IdRegistro = IdRegistro,
                IdEmpleado = IdEmpleado,
                Fecha = dia,
                Entrada = Combinar(dia, HoraEntrada),
                SalidaComida = Combinar(dia, HoraSalidaComida),
                RegresoComida = Combinar(dia, HoraRegresoComida),
                Salida = Combinar(dia, HoraSalida),
                ModificadoPor = ModificadoPor
            };

            AsistenciaNegocio negocio = new();
            bool modificado = negocio.Modificar(registro);

            if (!modificado)
                return NotFound();

            TempData["Mensaje"] = "Asistencia corregida correctamente.";
            return RedirectToPage("./Index");
        }
        catch (ArgumentException ex)
        {
            RecargarEstado(IdRegistro);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (ReglaNegocioException ex)
        {
            RecargarEstado(IdRegistro);
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            RecargarEstado(IdRegistro);
            ModelState.AddModelError(string.Empty, "No fue posible corregir la asistencia.");
            return Page();
        }
    }

    private static DateTime? Combinar(DateTime fecha, TimeSpan? hora)
        => hora.HasValue ? fecha.Date.Add(hora.Value) : null;

    private void RecargarEstado(int id)
    {
        try
        {
            AsistenciaNegocio negocio = new();
            var reg = negocio.BuscarPorId(id);
            if (reg is not null)
            {
                SemanaCerrada = reg.SemanaCerrada;
                NombreEmpleado = reg.NombreEmpleado;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }

    private void CargarCatalogos()
    {
        PersonalNegocio personalNegocio = new();
        ListaEmpleados = personalNegocio.Listar()
            .Select(e => new SelectListItem
            {
                Value = e.IdPersonal.ToString(),
                Text = $"#{e.IdPersonal} - {e.Nombre} {e.ApellidoPaterno} {e.ApellidoMaterno}".Trim()
            })
            .ToList();

        UsuarioNegocio usuarioNegocio = new();
        ListaUsuarios = usuarioNegocio.ListarActivos()
            .Select(u => new SelectListItem
            {
                Value = u.IdUsuario.ToString(),
                Text = $"{u.NombreCompleto} ({u.Rol})"
            })
            .ToList();
    }
}