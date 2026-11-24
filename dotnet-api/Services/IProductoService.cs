using Comisariato.Api.DTOs;

namespace Comisariato.Api.Services;

public interface IProductoService
{
    Task<IReadOnlyList<ProductoStockBajoResponse>> ObtenerStockBajoAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MovimientoInventarioResponse>> ObtenerMovimientosAsync(
        int productoId,
        CancellationToken cancellationToken);
}
