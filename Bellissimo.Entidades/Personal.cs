using System.ComponentModel.DataAnnotations;

namespace Bellissimo.Entidades;

public class Personal
{
    public int IdPersonal { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60, ErrorMessage = "El nombre no puede exceder los 60 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido paterno es obligatorio.")]
    [StringLength(60, ErrorMessage = "El apellido paterno no puede exceder los 60 caracteres.")]
    [Display(Name = "Apellido paterno")]
    public string ApellidoPaterno { get; set; } = string.Empty;

    [StringLength(60, ErrorMessage = "El apellido materno no puede exceder los 60 caracteres.")]
    [Display(Name = "Apellido materno")]
    public string? ApellidoMaterno { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede exceder los 20 caracteres.")]
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "Seleccione un puesto.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un puesto válido.")]
    [Display(Name = "Puesto")]
    public int IdPuesto { get; set; }

    [Display(Name = "Puesto")]
    public string? NombrePuesto { get; set; }

    [Required(ErrorMessage = "El sueldo base semanal es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El sueldo base semanal debe ser mayor a 0.")]
    [Display(Name = "Sueldo base semanal")]
    public decimal SueldoBase { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;

    [Display(Name = "Fecha de registro")]
    public DateTime FechaRegistro { get; set; }

    // true si ya tiene clave personal para el checador (se guarda sólo el hash).
    [Display(Name = "Clave del checador")]
    public bool TieneClave { get; set; }
}

public class Puesto
{
    public int IdPuesto { get; set; }

    [Required(ErrorMessage = "El nombre del puesto es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre del puesto no puede exceder los 50 caracteres.")]
    [Display(Name = "Puesto")]
    public string Nombre { get; set; } = string.Empty;
}