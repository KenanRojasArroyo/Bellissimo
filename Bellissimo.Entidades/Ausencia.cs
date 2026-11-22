using System.ComponentModel.DataAnnotations;

namespace Bellissimo.Entidades;

public class Ausencia
{
    public int IdAusencia { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un empleado.")]
    [Display(Name = "Empleado")]
    public int IdEmpleado { get; set; }

    [Display(Name = "Empleado")]
    public string? NombreEmpleado { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Seleccione el tipo de ausencia.")]
    [Display(Name = "Tipo")]
    public string Tipo { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Las observaciones no pueden exceder los 200 caracteres.")]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }

    // true si el pago de esa semana ya está cerrado: la ausencia es de solo lectura.
    [Display(Name = "Semana cerrada")]
    public bool SemanaCerrada { get; set; }
}

public static class TiposAusencia
{
    public const string Justificada = "Justificada";
    public const string Injustificada = "Injustificada";
    public const string Vacaciones = "Vacaciones";

    public static readonly string[] Todos = { Justificada, Injustificada, Vacaciones };
}
