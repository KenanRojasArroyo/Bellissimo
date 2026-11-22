using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bellissimo.Web.Pages.Puestos;

public class IndexModel : PageModel
{
    public List<Puesto> ListaPuestos { get; private set; } = new();

    public string? Error { get; private set; }

    public void OnGet()
    {
        try
        {
            PuestoNegocio negocio = new();
            ListaPuestos = negocio.Listar();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar los puestos. Verifique la conexión con MySQL.";
        }
    }
}