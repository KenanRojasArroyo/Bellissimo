using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Bellissimo.Entidades;

namespace Bellissimo.Web.Pages.Personal;

public class NuevoModel : PageModel
{
    [BindProperty]
    public Bellissimo.Entidades.Personal Empleado { get; set; } = new();

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        // PRÁCTICA 6:
        // PersonalNegocio negocio = new();
        // negocio.Agregar(Empleado);

        TempData["Mensaje"] =
            "Formulario validado. La persistencia se conectará en la Práctica 6.";

        return RedirectToPage("./Index");
    }
}