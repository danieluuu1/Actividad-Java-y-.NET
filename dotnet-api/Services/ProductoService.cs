using Comisariato.Api.DTOs;
using Comisariato.Api.Infrastructure;
using Comisariato.Api.Infrastructure.Exceptions;
using Comisariato.Api.Repositories;

namespace Comisariato.Api.Services;

public sealed class ProductoService : IProductoService
{
    private readonly IProductoRepository _repository;
    private readonly PollyPolicies _pollyPolicies;
    private readonly ILogger<ProductoService> _logger;

    public ProductoService(
        IProductoRepository repository,
        PollyPolicies pollyPolicies,
        ILogger<ProductoService> logger)
    {
        _repository = repository;
        _pollyPolicies = pollyPolicies;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ProductoStockBajoResponse>> ObtenerStockBajoAsync(
        CancellationToken cancellationToken)
    {
        var productos = await _pollyPolicies.DeadlockRetry.ExecuteAsync(
            token => _repository.ObtenerStockBajoAsync(token),
            cancellationToken);

        _logger.LogInformation(
            "Consulta de productos con stock bajo: {Cantidad} registros.",
            productos.Count);

        return productos;
    }

    public async Task<IReadOnlyList<MovimientoInventarioResponse>> ObtenerMovimientosAsync(
        int productoId,
        CancellationToken cancellationToken)
    {
        var existe = await _pollyPolicies.DeadlockRetry.ExecuteAsync(
            token => _repository.ExisteAsync(productoId, token),
            cancellationToken);

        if (!existe)
        {
            throw new NotFoundException($"El producto con ID {productoId} no existe.");
        }

        var movimientos = await _pollyPolicies.DeadlockRetry.ExecuteAsync(
            token => _repository.ObtenerMovimientosAsync(productoId, token),
            cancellationToken);

        _logger.LogInformation(
            "Consulta de movimientos del producto {ProductoID}: {Cantidad} registros.",
            productoId,
            movimientos.Count);

        return movimientos;
    }
}
