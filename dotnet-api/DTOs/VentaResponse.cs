namespace Comisariato.Api.DTOs;

/// <summary>
/// Respuesta de una venta registrada o consultada. Los importes provienen
/// de la base de datos, nunca de la solicitud del cliente.
/// </summary>
public sealed record VentaResponse(
    int VentaID,
    string NumeroFactura,
    int? ClienteID,
    int EmpleadoID,
    DateTime FechaVenta,
    decimal Subtotal,
    decimal IVA,
    decimal Total,
    string FormaPago,
    string Estado,
    IReadOnlyList<VentaDetalleResponse> Detalle);
