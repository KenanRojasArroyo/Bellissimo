using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bellissimo.Web.Pages.Personal;

public class NuevoModel : PageModel
{
    [BindProperty]
    public Bellissimo.Entidades.Personal Empleado { get; set; } = new();

    public List<SelectListItem> ListaPuestos { get; private set; } = new();

    public bool HayPuestos => ListaPuestos.Count > 0;

    public string? Error { get; private set; }

    public void OnGet()
    {
        try
        {
            CargarPuestos();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible cargar los puestos. Verifique la conexión con MySQL.";
        }
    }

    public IActionResult OnPost()
    {
        try
        {
            CargarPuestos();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ModelState.AddModelError(string.Empty, "No fue posible cargar los puestos.");
            return Page();
        }

        if (!HayPuestos)
        {
            ModelState.AddModelError(string.Empty, "No existen puestos registrados en la base de datos.");
            return Page();
        }

        if (Empleado.IdPuesto <= 0)
        {
            ModelState.AddModelError("Empleado.IdPuesto", "Seleccione un puesto válido.");
        }

        if (!ModelState.IsValid)
            return Page();

        try
        {
            PersonalNegocio negocio = new();
            int id = negocio.Agregar(Empleado);

            TempData["Mensaje"] = $"Empleado registrado correctamente. ID: {id}";
            return RedirectToPage("./Index");
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            ModelState.AddModelError(string.Empty, "No fue posible registrar el empleado.");
            return Page();
        }
    }

    private void CargarPuestos()
    {
        // Si Backend ya creó PuestoNegocio (o PersonalNegocio.ListarPuestos()), descomenta las siguientes líneas:
        // PuestoNegocio puestoNegocio = new();
        // ListaPuestos = puestoNegocio.Listar()
        //     .Select(p => new SelectListItem { Value = p.IdPuesto.ToString(), Text = p.Nombre })
        //     .ToList();

        ListaPuestos = new List<SelectListItem>
        {
            new() { Value = "1", Text = "Recepcionista" },
            new() { Value = "2", Text = "Repartidor" },
            new() { Value = "3", Text = "Encargado de Florista" }
        };
    }
}