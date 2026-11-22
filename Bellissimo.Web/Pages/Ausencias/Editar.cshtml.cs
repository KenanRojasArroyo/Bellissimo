using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bellissimo.Web.Pages.Ausencias;

public class EditarModel : PageModel
{
    [BindProperty]
    public Ausencia Ausencia { get; set; } = new();

    public List<SelectListItem> ListaEmpleados { get; private set; } = new();

    public List<SelectListItem> ListaTipos { get; private set; } = new();

    public string? Error { get; private set; }

    public IActionResult OnGet(int id)
    {
        try
        {
            CargarCatalogos();
            AusenciaNegocio negocio = new();
            Ausencia? ausencia = negocio.BuscarPorId(id);

            if (ausencia is null)
                return NotFound();

            Ausencia = ausencia;
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar la ausencia. Verifique la conexión con MySQL.";
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
            return Page();

        try
        {
            AusenciaNegocio negocio = new();
            bool modificado = negocio.Modificar(Ausencia);

            if (!modificado)
                return NotFound();

            TempData["Mensaje"] = "Ausencia modificada correctamente.";
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
            ModelState.AddModelError(string.Empty, "No fue posible modificar la ausencia.");
            return Page();
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

        ListaTipos = AusenciaNegocio.Tipos
            .Select(t => new SelectListItem { Value = t, Text = t })
            .ToList();
    }
}