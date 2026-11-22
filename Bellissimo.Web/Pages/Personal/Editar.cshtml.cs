using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bellissimo.Web.Pages.Personal;

public class EditarModel : PageModel
{
    [BindProperty]
    public Bellissimo.Entidades.Personal Empleado { get; set; } = new();

    public List<SelectListItem> ListaPuestos { get; private set; } = new();

    public string? Error { get; private set; }

    public IActionResult OnGet(int id)
    {
        try
        {
            CargarPuestos();

            PersonalNegocio negocio = new();
            Bellissimo.Entidades.Personal? empleado = negocio.BuscarPorId(id);

            if (empleado is null)
                return NotFound();

            Empleado = empleado;
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar el empleado. Verifique la conexión con MySQL.";
            return Page();
        }
    }

    public IActionResult OnPost()
    {
        try
        {
            CargarPuestos();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ModelState.AddModelError(string.Empty, "No fue posible cargar los puestos.");
            return Page();
        }

        if (Empleado.IdPuesto <= 0)
        {
            ModelState.AddModelError("Empleado.IdPuesto", "Seleccione un puesto válido.");
        }

        if (!ModelState.IsValid)
            return Page();

        try
        {
            PersonalNegocio negocio = new();
            bool modificado = negocio.Modificar(Empleado);

            if (!modificado)
                return NotFound();

            TempData["Mensaje"] = "Empleado modificado correctamente.";
            return RedirectToPage("./Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ModelState.AddModelError(string.Empty, "No fue posible modificar el empleado.");
            return Page();
        }
    }

    private void CargarPuestos()
    {
        ListaPuestos = new List<SelectListItem>
        {
            new() { Value = "1", Text = "Recepcionista" },
            new() { Value = "2", Text = "Repartidor" },
            new() { Value = "3", Text = "Encargado de Florista" }
        };
    }
}