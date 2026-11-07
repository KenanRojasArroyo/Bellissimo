using System.ComponentModel.DataAnnotations;

namespace Bellissimo.Entidades;

public class Personal
{
    public int IdPersonal { get; set; }

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

    [Required(ErrorMessage = "El tipo de cargo es obligatorio.")]
    public TipoCargo TipoCargo { get; set; }

    [Required(ErrorMessage = "El sueldo base es obligatorio.")]
    [Range(0, double.MaxValue, ErrorMessage = "El sueldo base debe ser mayor a 0.")]
    [Display(Name = "Sueldo base / hr")]
    public decimal SueldoBase { get; set; }

    public DateTime FechaRegistro { get; set; }
}

public enum TipoCargo
{
    EncargadoGeneral,
    EncargadoFlorista,
    Florista,
    Repartidor
}

public static class TipoCargoExtensions
{
    public static string ObtenerNombreAmigable(this TipoCargo cargo)
    {
        return cargo switch
        {
            TipoCargo.EncargadoGeneral => "Encargado General",
            TipoCargo.EncargadoFlorista => "Encargado de Florista",
            TipoCargo.Florista => "Florista",
            TipoCargo.Repartidor => "Repartidor",
            _ => cargo.ToString()
        };
    }
}