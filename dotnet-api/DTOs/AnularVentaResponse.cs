namespace Comisariato.Api.DTOs;

public sealed record AnularVentaResponse(
    int VentaID,
    string Estado,
    string Mensaje);
