using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bellissimo.Web.Pages.Personal;

public class IndexModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    public List<Bellissimo.Entidades.Personal> ListaPersonal { get; private set; } = new();

    public string? Error { get; private set; }

    public void OnGet()
    {
        try
        {
            PersonalNegocio negocio = new();

            ListaPersonal = string.IsNullOrWhiteSpace(Buscar)
                ? negocio.Listar()
                : negocio.Buscar(Buscar.Trim());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            Error = "No fue posible consultar el personal. Verifique la conexión con MySQL.";
        }
    }
}