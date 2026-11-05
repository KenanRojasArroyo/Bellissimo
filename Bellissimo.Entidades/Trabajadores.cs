using System.ComponentModel.DataAnnotations;

namespace Bellissimo.Entidades;

public class Cliente
{
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido paterno es obligatorio.")]
    [StringLength(60)]
    public string ApellidoPaterno { get; set; } = string.Empty;

    [StringLength(60)]
    public string? ApellidoMaterno { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(120)]
    public string? Correo { get; set; }

    [Required(ErrorMessage = "El tipo de cargo es obligatorio.")]
    public TipoCargo TipoCargo { get; set; }

    [Required(ErrorMessage = "El sueldo base es obligatorio.")]
    [Range(0, double.MaxValue, ErrorMessage = "El sueldo base debe ser mayor a 0.")]
    public decimal SueldoBase { get; set; }

    public DateTime FechaRegistro { get; set; }
}

public enum TipoCargo
{
    EncargadoGeneral,
    EncargadoDeFlorista
}