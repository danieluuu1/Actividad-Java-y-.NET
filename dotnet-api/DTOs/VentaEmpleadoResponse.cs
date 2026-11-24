namespace Comisariato.Api.DTOs;

public sealed record VentaEmpleadoResponse(
    int VentaID,
    string NumeroFactura,
    DateTime FechaVenta,
    decimal Total);
