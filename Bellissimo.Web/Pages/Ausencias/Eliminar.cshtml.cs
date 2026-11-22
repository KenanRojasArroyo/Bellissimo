using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bellissimo.Web.Pages.Ausencias;

public class EliminarModel : PageModel
{
    public Ausencia? Ausencia { get; private set; }

    public string? Error { get; private set; }

    public IActionResult OnGet(int id)
    {
        try
        {
            AusenciaNegocio negocio = new();
            Ausencia = negocio.BuscarPorId(id);

            if (Ausencia is null)
                return NotFound();

            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar la ausencia. Verifique la conexión con MySQL.";
            return Page();
        }
    }

    public IActionResult OnPost(int id)
    {
        try
        {
            AusenciaNegocio negocio = new();
            bool eliminado = negocio.Eliminar(id);

            if (!eliminado)
                return NotFound();

            TempData["Mensaje"] = "Ausencia eliminada correctamente.";
            return RedirectToPage("./Index");
        }
        catch (ReglaNegocioException ex)
        {
            Error = ex.Message;
            RecargarAusencia(id);
            return Page();
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
            RecargarAusencia(id);
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible eliminar la ausencia.";
            RecargarAusencia(id);
            return Page();
        }
    }

    private void RecargarAusencia(int id)
    {
        try
        {
            AusenciaNegocio negocio = new();
            Ausencia = negocio.BuscarPorId(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}