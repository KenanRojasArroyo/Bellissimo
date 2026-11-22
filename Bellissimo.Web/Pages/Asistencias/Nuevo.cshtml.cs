using System.ComponentModel.DataAnnotations;
using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bellissimo.Web.Pages.Asistencias;

public class NuevoModel : PageModel
{
    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un empleado.")]
    public int IdEmpleado { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    public DateTime Fecha { get; set; } = DateTime.Today;

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

    public List<SelectListItem> ListaEmpleados { get; private set; } = new();

    public List<SelectListItem> ListaUsuarios { get; private set; } = new();

    public string? Error { get; private set; }

    public void OnGet()
    {
        try
        {
            CargarCatalogos();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible cargar los catálogos.";
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
            return Page();

        try
        {
            DateTime dia = Fecha.Date;

            RegistroAsistencia registro = new()
            {
                IdEmpleado = IdEmpleado,
                Fecha = dia,
                Entrada = Combinar(dia, HoraEntrada),
                SalidaComida = Combinar(dia, HoraSalidaComida),
                RegresoComida = Combinar(dia, HoraRegresoComida),
                Salida = Combinar(dia, HoraSalida),
                ModificadoPor = ModificadoPor
            };

            AsistenciaNegocio negocio = new();
            negocio.Agregar(registro);

            TempData["Mensaje"] = "Asistencia capturada manualmente correctamente.";
            return RedirectToPage("./Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ModelState.AddModelError(string.Empty, "No fue posible registrar la asistencia.");
            return Page();
        }
    }

    private static DateTime? Combinar(DateTime fecha, TimeSpan? hora)
        => hora.HasValue ? fecha.Date.Add(hora.Value) : null;

    private void CargarCatalogos()
    {
        PersonalNegocio personalNegocio = new();
        ListaEmpleados = personalNegocio.Listar()
            .Where(e => e.Activo)
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