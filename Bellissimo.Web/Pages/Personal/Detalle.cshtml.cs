using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Bellissimo.Entidades;

namespace Bellissimo.Web.Pages.Personal;

public class DetalleModel : PageModel
{
    public Bellissimo.Entidades.Personal Empleado { get; private set; } = new();

    public IActionResult OnGet(int id)
    {
        Empleado = id switch
        {
            1 => new Bellissimo.Entidades.Personal
            {
                IdPersonal = 1,
                Nombre = "Ana",
                ApellidoPaterno = "López",
                ApellidoMaterno = "García",
                Telefono = "6461234567",
                TipoCargo = TipoCargo.EncargadoFlorista,
                SueldoBase = 60m,
                FechaRegistro = DateTime.Now
            },
            2 => new Bellissimo.Entidades.Personal
            {
                IdPersonal = 2,
                Nombre = "Carlos",
                ApellidoPaterno = "Ramírez",
                ApellidoMaterno = "Torres",
                Telefono = "6467654321",
                TipoCargo = TipoCargo.EncargadoGeneral,
                SueldoBase = 60m,
                FechaRegistro = DateTime.Now
            },
            _ => new Bellissimo.Entidades.Personal()
        };

        return Empleado.IdPersonal == 0 ? NotFound() : Page();
    }
}