using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Bellissimo.Entidades;

namespace Bellissimo.Web.Pages.Personal;

public class EditarModel : PageModel
{
    [BindProperty]
    public Bellissimo.Entidades.Personal Empleado { get; set; } = new();

    public IActionResult OnGet(int id)
    {
        // Datos temporales para comprobar la interfaz.
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
                SueldoBase = 6000m,
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
                SueldoBase = 15000m,
                FechaRegistro = DateTime.Now
            },
            _ => new Bellissimo.Entidades.Personal()
        };

        if (Empleado.IdPersonal == 0)
            return NotFound();

        return Page();
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
            return Page();

        // PRÁCTICA 6:
        // new ClienteNegocio().Modificar(Cliente);

        TempData["Mensaje"] = "Cambios validados correctamente.";
        return RedirectToPage("./Index");
    }
}