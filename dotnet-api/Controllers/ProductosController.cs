using Comisariato.Api.DTOs;
using Comisariato.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comisariato.Api.Controllers;

[ApiController]
[Route("api/productos")]
[Produces("application/json")]
public sealed class ProductosController : ControllerBase
{
    private readonly IProductoService _productoService;

    public ProductosController(IProductoService productoService)
    {
        _productoService = productoService;
    }

    /// <summary>
    /// Lista los productos cuyo stock es menor o igual al stock mínimo
    /// (vista dbo.vw_ProductosStockBajo).
    /// </summary>
    [HttpGet("stock-bajo")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductoStockBajoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ProductoStockBajoResponse>>> ObtenerStockBajo(
        CancellationToken cancellationToken)
    {
        var productos = await _productoService.ObtenerStockBajoAsync(cancellationToken);
        return Ok(productos);
    }

    /// <summary>
    /// Lista los movimientos de inventario de un producto. Devuelve 404 si
    /// el producto no existe.
    /// </summary>
    [HttpGet("{productoId:int}/movimientos")]
    [ProducesResponseType(typeof(IReadOnlyList<MovimientoInventarioResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<MovimientoInventarioResponse>>> ObtenerMovimientos(
        int productoId,
        CancellationToken cancellationToken)
    {
        var movimientos = await _productoService.ObtenerMovimientosAsync(productoId, cancellationToken);
        return Ok(movimientos);
    }
}
