using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bellissimo.Web.Pages.Ausencias;

public class IndexModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? IdEmpleado { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? Desde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? Hasta { get; set; }

    public List<Ausencia> ListaAusencias { get; private set; } = new();

    public List<SelectListItem> ListaEmpleados { get; private set; } = new();

    public string? Error { get; private set; }

    public void OnGet()
    {
        try
        {
            PersonalNegocio personalNegocio = new();
            ListaEmpleados = personalNegocio.Listar()
                .Select(e => new SelectListItem
                {
                    Value = e.IdPersonal.ToString(),
                    Text = $"#{e.IdPersonal} - {e.Nombre} {e.ApellidoPaterno} {e.ApellidoMaterno}".Trim()
                })
                .ToList();

            AusenciaNegocio negocio = new();
            ListaAusencias = negocio.Listar(IdEmpleado, Desde, Hasta);
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar las ausencias. Verifique la conexión con MySQL.";
        }
    }
}