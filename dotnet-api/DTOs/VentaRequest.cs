using System.ComponentModel.DataAnnotations;

namespace Comisariato.Api.DTOs;

/// <summary>
/// Datos de entrada para registrar una venta. El Total, Subtotal e IVA
/// son calculados por el procedimiento almacenado usp_RegistrarVenta.
/// </summary>
public sealed record VentaRequest
{
    [Required(ErrorMessage = "El NumeroFactura es obligatorio.")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "El NumeroFactura debe tener entre 1 y 20 caracteres.")]
    public string NumeroFactura { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "El ClienteID debe ser mayor que cero.")]
    public int? ClienteID { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "El EmpleadoID debe ser mayor que cero.")]
    public int EmpleadoID { get; init; }

    [Required(ErrorMessage = "La FormaPago es obligatoria.")]
    [RegularExpression("^(Efectivo|Tarjeta|Transferencia)$",
        ErrorMessage = "La FormaPago debe ser Efectivo, Tarjeta o Transferencia.")]
    public string FormaPago { get; init; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "El ProductoID debe ser mayor que cero.")]
    public int ProductoID { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "La Cantidad debe ser mayor que cero.")]
    public int Cantidad { get; init; }
}
