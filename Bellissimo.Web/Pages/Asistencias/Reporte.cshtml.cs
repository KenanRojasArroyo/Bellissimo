using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bellissimo.Web.Pages.Asistencias;

public class ReporteModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public DateTime? Desde { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? Hasta { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdEmpleado { get; set; }

    public List<AsistenciaDiaria> Filas { get; private set; } = new();

    public List<SelectListItem> ListaEmpleados { get; private set; } = new();

    public string? Error { get; private set; }

    public void OnGet()
    {
        DateTime hoy = DateTime.Today;
        int diff = (7 + (hoy.DayOfWeek - DayOfWeek.Monday)) % 7;
        Desde ??= hoy.AddDays(-diff);
        Hasta ??= Desde.Value.AddDays(5);

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

            AsistenciaNegocio negocio = new();
            Filas = negocio.ReporteDiario(Desde.Value, Hasta.Value, IdEmpleado);
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar el reporte diario. Verifique la conexión con MySQL.";
        }
    }
}