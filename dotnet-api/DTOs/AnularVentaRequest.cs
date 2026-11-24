using System.ComponentModel.DataAnnotations;

namespace Comisariato.Api.DTOs;

/// <summary>
/// Cuerpo opcional para anular una venta. El EmpleadoID se usa para
/// registrar la bitácora de devolución de stock.
/// </summary>
public sealed record AnularVentaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "El EmpleadoID debe ser mayor que cero.")]
    public int? EmpleadoID { get; init; }
}
