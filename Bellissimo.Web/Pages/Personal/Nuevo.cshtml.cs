using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bellissimo.Entidades;

namespace Bellissimo.Web.Pages.Personal;

public class NuevoModel : PageModel
{
    [BindProperty]
    public Bellissimo.Entidades.Personal Empleado { get; set; } = new();

    public List<SelectListItem> ListaPuestos { get; private set; } = new();

    public void OnGet()
    {
        CargarPuestos();
    }

    public IActionResult OnPost()
    {
        if (Empleado.IdPuesto <= 0)
        {
            ModelState.AddModelError("Empleado.IdPuesto", "Seleccione un puesto válido.");
        }

        if (!ModelState.IsValid)
        {
            CargarPuestos();
            return Page();
        }

        // PRÁCTICA 6:
        // PersonalNegocio negocio = new();
        // negocio.Agregar(Empleado);

        TempData["Mensaje"] =
            "Formulario validado. La persistencia se conectará en la Práctica 6.";

        return RedirectToPage("./Index");
    }

    private void CargarPuestos()
    {
        // Catálogo temporal alineado con la tabla 'puesto' de la BD (Recepcionista, Repartidor, Encargado de Florista)
        ListaPuestos = new List<SelectListItem>
        {
            new() { Value = "1", Text = "Recepcionista" },
            new() { Value = "2", Text = "Repartidor" },
            new() { Value = "3", Text = "Encargado de Florista" }
        };
    }
}