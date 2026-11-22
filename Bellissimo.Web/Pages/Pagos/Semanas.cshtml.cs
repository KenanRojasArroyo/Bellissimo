using System.ComponentModel.DataAnnotations;
using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bellissimo.Web.Pages.Pagos;

public class SemanasModel : PageModel
{
    [BindProperty]
    [Required(ErrorMessage = "Seleccione la fecha de inicio (lunes).")]
    [DataType(DataType.Date)]
    public DateTime FechaInicio { get; set; } = SemanaNegocio.LunesDe(DateTime.Today);

    public List<Semana> ListaSemanas { get; private set; } = new();

    public string? Error { get; private set; }

    public void OnGet()
    {
        CargarSemanas();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            CargarSemanas();
            return Page();
        }

        try
        {
            SemanaNegocio negocio = new();
            negocio.Agregar(FechaInicio);

            TempData["Mensaje"] = "Semana registrada correctamente.";
            return RedirectToPage("./Semanas");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (ReglaNegocioException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ModelState.AddModelError(string.Empty, "No fue posible registrar la semana.");
        }

        CargarSemanas();
        return Page();
    }

    private void CargarSemanas()
    {
        try
        {
            SemanaNegocio negocio = new();
            ListaSemanas = negocio.Listar();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar las semanas. Verifique la conexión con MySQL.";
        }
    }
}