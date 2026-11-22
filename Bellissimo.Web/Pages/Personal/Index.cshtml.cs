using Bellissimo.Entidades;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bellissimo.Web.Pages.Personal;

public class IndexModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    public List<Bellissimo.Entidades.Personal> ListaPersonal { get; private set; } = new();

    public void OnGet()
    {
        ListaPersonal = new List<Bellissimo.Entidades.Personal>
        {
            new()
            {
                IdPersonal = 1,
                Nombre = "Ana",
                ApellidoPaterno = "López",
                ApellidoMaterno = "García",
                Telefono = "6461234567",
                NombrePuesto = "Encargado de Florista",
                SueldoBase = 3300m,
                Activo = true,
                FechaRegistro = DateTime.Now,
            },
            new()
            {
                IdPersonal = 2,
                Nombre = "Carlos",
                ApellidoPaterno = "Ramírez",
                ApellidoMaterno = "Torres",
                Telefono = "6467654321",
                NombrePuesto = "Encargado General",
                SueldoBase = 3000m,
                Activo = true,
                FechaRegistro = DateTime.Now,
            }
        };

        if (!string.IsNullOrWhiteSpace(Buscar))
        {
            string texto = Buscar.Trim();

            ListaPersonal = ListaPersonal
                .Where(p =>
                    p.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    p.ApellidoPaterno.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    (p.ApellidoMaterno != null && p.ApellidoMaterno.Contains(texto, StringComparison.OrdinalIgnoreCase)) ||
                    (p.NombrePuesto != null && p.NombrePuesto.Contains(texto, StringComparison.OrdinalIgnoreCase)) ||
                    p.Telefono.Contains(texto, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}