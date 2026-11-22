using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bellissimo.Web.Pages.Asistencias;

public class EliminarModel : PageModel
{
    public RegistroAsistencia? Registro { get; private set; }

    public string? Error { get; private set; }

    public IActionResult OnGet(int id)
    {
        try
        {
            AsistenciaNegocio negocio = new();
            Registro = negocio.BuscarPorId(id);

            if (Registro is null)
                return NotFound();

            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar el registro de asistencia. Verifique la conexión con MySQL.";
            return Page();
        }
    }

    public IActionResult OnPost(int id)
    {
        try
        {
            AsistenciaNegocio negocio = new();
            bool eliminado = negocio.Eliminar(id);

            if (!eliminado)
                return NotFound();

            TempData["Mensaje"] = "Registro de asistencia eliminado correctamente.";
            return RedirectToPage("./Index");
        }
        catch (ReglaNegocioException ex)
        {
            Error = ex.Message;
            RecargarRegistro(id);
            return Page();
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
            RecargarRegistro(id);
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible eliminar el registro de asistencia.";
            RecargarRegistro(id);
            return Page();
        }
    }

    private void RecargarRegistro(int id)
    {
        try
        {
            AsistenciaNegocio negocio = new();
            Registro = negocio.BuscarPorId(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }
    }
}