namespace Comisariato.Api.DTOs;

public sealed record ProductoStockBajoResponse(
    int ProductoID,
    string Nombre,
    int Stock,
    int StockMinimo,
    string Categoria);
