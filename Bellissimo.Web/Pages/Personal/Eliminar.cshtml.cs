using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bellissimo.Web.Pages.Personal;

public class EliminarModel : PageModel
{
    public Bellissimo.Entidades.Personal Empleado { get; private set; } = new();

    public string? Error { get; private set; }

    public IActionResult OnGet(int id)
    {
        if (id <= 0)
            return NotFound();

        try
        {
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

    public IActionResult OnPost(int id)
    {
        PersonalNegocio negocio = new();
        Bellissimo.Entidades.Personal? empleado = negocio.BuscarPorId(id);

        if (empleado is null)
            return NotFound();

        Empleado = empleado;

        try
        {
            bool eliminado = negocio.Eliminar(id);

            if (!eliminado)
                return NotFound();

            TempData["Mensaje"] = "Empleado eliminado correctamente.";
            return RedirectToPage("./Index");
        }
        catch (ReglaNegocioException ex)
        {
            Error = ex.Message;
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible eliminar el empleado.";
            return Page();
        }
    }
}