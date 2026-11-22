using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Bellissimo.Entidades;

namespace Bellissimo.Web.Pages.Personal;

public class EditarModel : PageModel
{
    [BindProperty]
    public Bellissimo.Entidades.Personal Empleado { get; set; } = new();

    public List<SelectListItem> ListaPuestos { get; private set; } = new();

    public IActionResult OnGet(int id)
    {
        CargarPuestos();

        // Datos temporales alineados con la BD (puestos: Recepcionista=1, Repartidor=2, Encargado de Florista=3; sueldos semanales $3,000-$3,300)
        Empleado = id switch
        {
            1 => new Bellissimo.Entidades.Personal
            {
                IdPersonal = 1,
                Nombre = "Ana",
                ApellidoPaterno = "López",
                ApellidoMaterno = "García",
                Telefono = "6461234567",
                IdPuesto = 3,
                NombrePuesto = "Encargado de Florista",
                SueldoBase = 3300m,
                Activo = true,
                FechaRegistro = DateTime.Now
            },
            2 => new Bellissimo.Entidades.Personal
            {
                IdPersonal = 2,
                Nombre = "Carlos",
                ApellidoPaterno = "Ramírez",
                ApellidoMaterno = "Torres",
                Telefono = "6467654321",
                IdPuesto = 1,
                NombrePuesto = "Recepcionista",
                SueldoBase = 3000m,
                Activo = true,
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
        if (Empleado.IdPuesto <= 0)
        {
            ModelState.AddModelError("Empleado.IdPuesto", "Seleccione un puesto válido.");
        }

        if (!ModelState.IsValid)
        {
            CargarPuestos();
            return Page();
        }

        // PRÁCTICA 6:
        // new PersonalNegocio().Modificar(Empleado);

        TempData["Mensaje"] = "Cambios validados correctamente.";
        return RedirectToPage("./Index");
    }

    private void CargarPuestos()
    {
        ListaPuestos = new List<SelectListItem>
        {
            new() { Value = "1", Text = "Recepcionista" },
            new() { Value = "2", Text = "Repartidor" },
            new() { Value = "3", Text = "Encargado de Florista" }
        };
    }
}