using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bellissimo.Web.Pages.Pagos;

public class IndexModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? IdSemana { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdEmpleado { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Estado { get; set; }

    [BindProperty]
    public int SemanaGenerar { get; set; }

    [BindProperty]
    public int? UsuarioGenerar { get; set; }

    public List<ResumenPago> ListaPagos { get; private set; } = new();

    public List<DiaSinChecada> PendientesSemana { get; private set; } = new();

    public List<SelectListItem> ListaSemanas { get; private set; } = new();

    public List<SelectListItem> ListaEmpleados { get; private set; } = new();

    public List<SelectListItem> ListaUsuarios { get; private set; } = new();

    public string? Error { get; private set; }

    public void OnGet()
    {
        try
        {
            CargarCatalogos();
            PagoNegocio negocio = new();
            ListaPagos = negocio.ListarResumen(IdSemana, IdEmpleado, Estado);

            if (IdSemana is > 0)
            {
                PendientesSemana = negocio.DiasSinChecada(IdSemana.Value, IdEmpleado);
            }
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar los pagos. Verifique la conexión con MySQL.";
        }
    }

    public IActionResult OnPostGenerarSemana()
    {
        try
        {
            PagoNegocio negocio = new();
            ResultadoGeneracionPagos res = negocio.GenerarSemana(SemanaGenerar, UsuarioGenerar);

            TempData["Mensaje"] = $"Pagos generados: {res.Generados}. Omitidos: {res.Omitidos.Count}.";
            if (res.Omitidos.Count > 0)
            {
                TempData["Omitidos"] = string.Join(" | ", res.Omitidos);
            }

            return RedirectToPage("./Index", new { IdSemana = SemanaGenerar });
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
        }
        catch (ReglaNegocioException ex)
        {
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible generar los pagos de la semana.";
        }

        try
        {
            CargarCatalogos();
            PagoNegocio negocio = new();
            ListaPagos = negocio.ListarResumen(IdSemana, IdEmpleado, Estado);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
        }

        return Page();
    }

    private void CargarCatalogos()
    {
        SemanaNegocio semanaNegocio = new();
        var semanas = semanaNegocio.Listar();
        ListaSemanas = semanas
            .Select(s => new SelectListItem
            {
                Value = s.IdSemana.ToString(),
                Text = s.Descripcion
            })
            .ToList();

        if (SemanaGenerar <= 0 && semanas.Count > 0)
        {
            SemanaGenerar = IdSemana ?? semanas[0].IdSemana;
        }

        PersonalNegocio personalNegocio = new();
        ListaEmpleados = personalNegocio.Listar()
            .Select(e => new SelectListItem
            {
                Value = e.IdPersonal.ToString(),
                Text = $"#{e.IdPersonal} - {e.Nombre} {e.ApellidoPaterno} {e.ApellidoMaterno}".Trim()
            })
            .ToList();

        UsuarioNegocio usuarioNegocio = new();
        ListaUsuarios = usuarioNegocio.ListarActivos()
            .Select(u => new SelectListItem
            {
                Value = u.IdUsuario.ToString(),
                Text = $"{u.NombreCompleto} ({u.Rol})"
            })
            .ToList();
    }
}