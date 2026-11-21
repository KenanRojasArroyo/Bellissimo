using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace Bellissimo.Web.Pages.RegistroSalidas
{
    public class RegistroSalidaModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Por favor, ingresa la matrícula del empleado.")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "La matrícula debe tener exactamente 6 dígitos.")] public string Matricula { get; set; } = string.Empty;

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

            // TODO: Agregar aquí la lógica para buscar al empleado y guardar la hora de entrada en la base de datos.

            MensajeExito = $"Entrada registrada correctamente para la matrícula: {Matricula} a las {DateTime.Now:HH:mm}.";

            // Redirige a OnGet para limpiar el formulario y dejar el cursor listo para el siguiente empleado
            return RedirectToPage();
        }
    }
}