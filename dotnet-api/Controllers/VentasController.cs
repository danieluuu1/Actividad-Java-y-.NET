using Comisariato.Api.DTOs;
using Comisariato.Api.Infrastructure.Exceptions;
using Comisariato.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Comisariato.Api.Controllers;

[ApiController]
[Route("api/ventas")]
[Produces("application/json")]
public sealed class VentasController : ControllerBase
{
    private readonly IVentaService _ventaService;

    public VentasController(IVentaService ventaService)
    {
        _ventaService = ventaService;
    }

    /// <summary>
    /// Registra una venta. El Subtotal, IVA y Total los calcula el
    /// procedimiento almacenado usp_RegistrarVenta en el servidor.
    /// </summary>
    /// <response code="201">Venta creada correctamente.</response>
    /// <response code="400">Falta un campo obligatorio o es inválido.</response>
    /// <response code="409">El NumeroFactura ya existe.</response>
    /// <response code="422">Regla de negocio violada (por ejemplo, stock insuficiente).</response>
    [HttpPost]
    [ProducesResponseType(typeof(VentaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<VentaResponse>> RegistrarVenta(
        [FromBody] VentaRequest request,
        CancellationToken cancellationToken)
    {
        var venta = await _ventaService.RegistrarVentaAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, venta);
    }

    /// <summary>
    /// Anula una venta y devuelve el stock de sus productos. El cuerpo es
    /// opcional; si se envía, permite registrar el EmpleadoID en la bitácora.
    /// También se acepta el EmpleadoID como query string.
    /// </summary>
    /// <response code="200">Venta anulada correctamente.</response>
    /// <response code="404">La venta no existe.</response>
    /// <response code="409">La venta ya está anulada (conflicto de estado).</response>
    [HttpPost("{ventaId:int}/anular")]
    [ProducesResponseType(typeof(AnularVentaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AnularVentaResponse>> AnularVenta(
        int ventaId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AnularVentaRequest? request,
        [FromQuery] int? empleadoId,
        CancellationToken cancellationToken)
    {
        if (empleadoId is <= 0)
        {
            throw new InputValidationException("El EmpleadoID debe ser mayor que cero.");
        }

        var empleado = request?.EmpleadoID ?? empleadoId;
        var response = await _ventaService.AnularVentaAsync(ventaId, empleado, cancellationToken);
        return Ok(response);
    }
}
