namespace Comisariato.Api.DTOs;

public sealed record MovimientoInventarioResponse(
    long MovimientoID,
    int ProductoID,
    string TipoMovimiento,
    int Cantidad,
    int StockAnterior,
    int StockNuevo,
    string? ReferenciaTipo,
    int? ReferenciaID,
    DateTime FechaMovimiento,
    int? EmpleadoID);
