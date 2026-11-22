using System.ComponentModel.DataAnnotations;

namespace Bellissimo.Entidades;

// Una fila por empleado y día con sus cuatro checadas (tabla registro_asistencia).
public class RegistroAsistencia
{
    public int IdRegistro { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un empleado.")]
    [Display(Name = "Empleado")]
    public int IdEmpleado { get; set; }

    [Display(Name = "Empleado")]
    public string? NombreEmpleado { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha")]
    public DateTime Fecha { get; set; } = DateTime.Today;

    [Display(Name = "Entrada")]
    public DateTime? Entrada { get; set; }

    [Display(Name = "Salida a comer")]
    public DateTime? SalidaComida { get; set; }

    [Display(Name = "Regreso de comer")]
    public DateTime? RegresoComida { get; set; }

    [Display(Name = "Salida")]
    public DateTime? Salida { get; set; }

    // Clave, Huella o Manual (las correcciones siempre quedan como Manual).
    [Display(Name = "Método")]
    public string Metodo { get; set; } = "Manual";

    // Usuario (dueño o encargado) que hizo la corrección.
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione quién hace la corrección.")]
    [Display(Name = "Corregido por")]
    public int? ModificadoPor { get; set; }

    [Display(Name = "Corregido por")]
    public string? NombreModificadoPor { get; set; }

    [Display(Name = "Semana cerrada")]
    public bool SemanaCerrada { get; set; }
}

// Fila de la vista v_asistencia_diaria (reporte de asistencia).
public class AsistenciaDiaria
{
    public int IdRegistro { get; set; }
    public int IdEmpleado { get; set; }
    public string Empleado { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public TimeSpan? Entrada { get; set; }
    public TimeSpan? Salida { get; set; }
    public decimal HorasPresentes { get; set; }
    public int MinComida { get; set; }
    public int MinComidaExtra { get; set; }
    public int MinRetardo { get; set; }
    public int MinSalidaTemprana { get; set; }
    public int MinExtra { get; set; }
    public string Metodo { get; set; } = string.Empty;
}
