using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bellissimo.Web.Pages.Puestos;

public class EditarModel : PageModel
{
    [BindProperty]
    public Puesto Puesto { get; set; } = new();

    public string? Error { get; private set; }

    public IActionResult OnGet(int id)
    {
        try
        {
            PuestoNegocio negocio = new();
            Puesto? puesto = negocio.BuscarPorId(id);

            if (puesto is null)
                return NotFound();

            Puesto = puesto;
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar el puesto. Verifique la conexión con MySQL.";
            return Page();
        }
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        try
        {
            PuestoNegocio negocio = new();
            bool modificado = negocio.Modificar(Puesto);

            if (!modificado)
                return NotFound();

            TempData["Mensaje"] = "Puesto modificado correctamente.";
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
            ModelState.AddModelError(string.Empty, "No fue posible modificar el puesto.");
            return Page();
        }
    }
}