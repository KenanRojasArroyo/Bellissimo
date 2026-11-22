using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bellissimo.Web.Pages.Pagos;

public class TicketModel : PageModel
{
    public Ticket? Ticket { get; private set; }

    public List<DiaSinChecada> DiasPendientes { get; private set; } = new();

    [BindProperty]
    public string? NuevaObservacion { get; set; }

    public string? Error { get; private set; }

    public IActionResult OnGet(int id)
    {
        return CargarVista(id);
    }

    public IActionResult OnPostCerrar(int id)
    {
        try
        {
            PagoNegocio negocio = new();
            bool cerrado = negocio.Cerrar(id);

            if (!cerrado)
                return NotFound();

            TempData["Mensaje"] = "Pago cerrado correctamente. Ya no podrá modificarse.";
            return RedirectToPage("./Ticket", new { id });
        }
        catch (ReglaNegocioException ex)
        {
            Error = ex.Message;
            return CargarVista(id);
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
            return CargarVista(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible cerrar el pago.";
            return CargarVista(id);
        }
    }

    public IActionResult OnPostObservacion(int id)
    {
        try
        {
            PagoNegocio negocio = new();
            bool guardada = negocio.AgregarObservacion(id, NuevaObservacion);

            if (!guardada)
                return NotFound();

            TempData["Mensaje"] = "Observación actualizada correctamente.";
            return RedirectToPage("./Ticket", new { id });
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
            return CargarVista(id);
        }
        catch (ReglaNegocioException ex)
        {
            Error = ex.Message;
            return CargarVista(id);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible guardar la observación.";
            return CargarVista(id);
        }
    }

    private IActionResult CargarVista(int id)
    {
        try
        {
            PagoNegocio negocio = new();
            Ticket = negocio.ObtenerTicketPorPago(id);

            if (Ticket is null)
                return NotFound();

            NuevaObservacion ??= Ticket.Observaciones;
            DiasPendientes = negocio.DiasSinChecada(Ticket.IdSemana, Ticket.IdEmpleado);
            return Page();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            Error = "No fue posible consultar el ticket de pago. Verifique la conexión con MySQL.";
            return Page();
        }
    }
}