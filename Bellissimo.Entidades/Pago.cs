using System.ComponentModel.DataAnnotations;

namespace Bellissimo.Entidades;

// Periodo de pago de lunes a sábado.
public class Semana
{
    public int IdSemana { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Inicio (lunes)")]
    public DateTime FechaInicio { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fin (sábado)")]
    public DateTime FechaFin { get; set; }

    public string Descripcion => $"{FechaInicio:dd/MM/yyyy} – {FechaFin:dd/MM/yyyy}";
}

// Fila de la vista v_resumen_pago_semanal.
public class ResumenPago
{
    public int IdPago { get; set; }
    public int IdSemana { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int IdEmpleado { get; set; }
    public string Empleado { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public decimal SueldoBase { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal MontoExtra { get; set; }
    public decimal SalarioFinal { get; set; }

    // Borrador o Cerrado
    public string Estado { get; set; } = string.Empty;

    public bool Cerrado => Estado == EstadosPago.Cerrado;
}

public static class EstadosPago
{
    public const string Borrador = "Borrador";
    public const string Cerrado = "Cerrado";
}

// Resultado de sp_ticket: resumen + renglones, tal como quedó guardado.
public class Ticket
{
    public int IdPago { get; set; }
    public int IdEmpleado { get; set; }
    public int IdSemana { get; set; }
    public string Empleado { get; set; } = string.Empty;
    public string Puesto { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal SueldoBase { get; set; }
    public decimal ValorHora { get; set; }
    public int MinExcesoComida { get; set; }
    public int MinRetardo { get; set; }
    public int MinSalidaTemprana { get; set; }
    public int MinFalta { get; set; }
    public int MinExtra { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal MontoExtra { get; set; }
    public decimal SalarioAPagar { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaGeneracion { get; set; }
    public string? GeneradoPor { get; set; }
    public string? Observaciones { get; set; }
    public List<RenglonTicket> Renglones { get; set; } = new();

    public bool Cerrado => Estado == EstadosPago.Cerrado;
}

public class RenglonTicket
{
    // null en conceptos semanales (por ejemplo, retardos acumulados).
    public DateTime? Fecha { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public int Minutos { get; set; }

    // negativo = descuento, 0 = informativo, positivo = pago
    public decimal Monto { get; set; }
}

// Fila de la vista v_dias_sin_checada.
public class DiaSinChecada
{
    public int IdSemana { get; set; }
    public int IdEmpleado { get; set; }
    public string Empleado { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int DiaSemana { get; set; }
    public int MinutosJornada { get; set; }
    public bool SemanaCerrada { get; set; }
}

public class ResultadoGeneracionPagos
{
    public int Generados { get; set; }

    // "Empleado: motivo" de los pagos que no se pudieron generar.
    public List<string> Omitidos { get; set; } = new();
}
