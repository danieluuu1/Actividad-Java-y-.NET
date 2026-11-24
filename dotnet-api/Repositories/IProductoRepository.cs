using Comisariato.Api.DTOs;

namespace Comisariato.Api.Repositories;

public interface IProductoRepository
{
    Task<IReadOnlyList<ProductoStockBajoResponse>> ObtenerStockBajoAsync(CancellationToken cancellationToken);

    Task<bool> ExisteAsync(int productoId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MovimientoInventarioResponse>> ObtenerMovimientosAsync(
        int productoId,
        CancellationToken cancellationToken);
}
