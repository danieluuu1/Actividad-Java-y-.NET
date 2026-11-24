using Comisariato.Api.DTOs;
using Comisariato.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Comisariato.Api.Controllers;

[ApiController]
[Route("api/empleados")]
[Produces("application/json")]
public sealed class EmpleadosController : ControllerBase
{
    private readonly IVentaService _ventaService;

    public EmpleadosController(IVentaService ventaService)
    {
        _ventaService = ventaService;
    }

    /// <summary>
    /// Lista las ventas de un empleado usando la función de tabla
    /// dbo.fn_VentasPorEmpleado. Devuelve 404 si el empleado no existe y un
    /// arreglo vacío si existe pero no tiene ventas.
    /// </summary>
    [HttpGet("{empleadoId:int}/ventas")]
    [ProducesResponseType(typeof(IReadOnlyList<VentaEmpleadoResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<VentaEmpleadoResponse>>> ObtenerVentasPorEmpleado(
        int empleadoId,
        CancellationToken cancellationToken)
    {
        var ventas = await _ventaService.ObtenerVentasPorEmpleadoAsync(empleadoId, cancellationToken);
        return Ok(ventas);
    }
}
