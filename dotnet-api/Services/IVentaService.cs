using Comisariato.Api.DTOs;

namespace Comisariato.Api.Services;

public interface IVentaService
{
    Task<VentaResponse> RegistrarVentaAsync(VentaRequest request, CancellationToken cancellationToken);

    Task<AnularVentaResponse> AnularVentaAsync(
        int ventaId,
        int? empleadoId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<VentaEmpleadoResponse>> ObtenerVentasPorEmpleadoAsync(
        int empleadoId,
        CancellationToken cancellationToken);
}
