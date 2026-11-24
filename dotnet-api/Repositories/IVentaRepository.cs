using Comisariato.Api.DTOs;
using Microsoft.Data.SqlClient;

namespace Comisariato.Api.Repositories;

public interface IVentaRepository
{
    Task<int> RegistrarVentaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        VentaRequest request,
        CancellationToken cancellationToken);

    Task<VentaResponse?> ObtenerVentaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int ventaId,
        CancellationToken cancellationToken);

    Task AnularVentaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int ventaId,
        int? empleadoId,
        CancellationToken cancellationToken);

    Task<bool> EmpleadoExisteAsync(int empleadoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<VentaEmpleadoResponse>> ObtenerVentasPorEmpleadoAsync(
        int empleadoId,
        CancellationToken cancellationToken);
}
