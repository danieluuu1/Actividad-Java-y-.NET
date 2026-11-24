namespace Comisariato.Api.DTOs;

public sealed record VentaDetalleResponse(
    int ProductoID,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Descuento,
    decimal SubtotalLinea);
