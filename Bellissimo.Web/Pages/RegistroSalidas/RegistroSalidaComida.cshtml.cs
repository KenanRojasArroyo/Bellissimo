using System.ComponentModel.DataAnnotations;
using Bellissimo.Entidades;
using Bellissimo.Negocios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Bellissimo.Web.Pages.RegistroSalidas
{
    public class RegistroSalidaComidaModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Por favor, ingresa tu número de empleado.")]
        [Range(1, int.MaxValue, ErrorMessage = "Ingresa un número de empleado válido.")]
        public int? IdEmpleado { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Por favor, ingresa tu clave personal.")]
        [RegularExpression(@"^\d{4,8}$", ErrorMessage = "La clave debe tener de 4 a 8 dígitos.")]
        public string Clave { get; set; } = string.Empty;

        [TempData]
        public string? MensajeExito { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                ChecadorNegocio negocio = new();
                ResultadoChecada resultado = negocio.Registrar(
                    IdEmpleado!.Value,
                    Clave,
                    TipoChecada.SalidaComida);

                MensajeExito = $"{resultado.Checada.Descripcion()} registrada para {resultado.NombreEmpleado} a las {resultado.Hora:HH:mm}.";
                return RedirectToPage();
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
            catch (ReglaNegocioException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                ModelState.AddModelError(string.Empty, "No fue posible registrar la salida a comer. Verifique la conexión con MySQL.");
                return Page();
            }
        }
    }
}