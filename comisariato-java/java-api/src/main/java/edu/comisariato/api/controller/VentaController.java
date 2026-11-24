package edu.comisariato.api.controller;
import jakarta.validation.Valid;
import jakarta.validation.constraints.Positive;
import edu.comisariato.api.dto.*;
import edu.comisariato.api.service.VentaService;
import io.swagger.v3.oas.annotations.Operation;
import io.swagger.v3.oas.annotations.responses.ApiResponse;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.annotation.*;
@RestController
@RequestMapping("/api/ventas")
public class VentaController {
    private final VentaService service;
    public VentaController(VentaService service) { this.service=service; }
    @Operation(summary="Registrar venta; el SP calcula precios, subtotal, IVA y total")
    @ApiResponse(responseCode="201",description="Venta creada")
    @ApiResponse(responseCode="400",description="Request invalido")
    @ApiResponse(responseCode="404",description="Cliente, empleado o producto inexistente")
    @ApiResponse(responseCode="409",description="Factura duplicada o deadlock agotado")
    @ApiResponse(responseCode="422",description="Stock insuficiente u otra regla de negocio")
    @PostMapping @ResponseStatus(HttpStatus.CREATED)
    public VentaResponse registrar(@Valid @RequestBody VentaRequest request) { return service.registrar(request); }
    @Operation(summary="Anular venta y restituir stock una sola vez")
    @ApiResponse(responseCode="200",description="Venta anulada")
    @ApiResponse(responseCode="404",description="Venta inexistente")
    @ApiResponse(responseCode="409",description="Venta ya anulada o estado incompatible")
    @PostMapping("/{ventaId}/anular")
    public VentaResponse anular(@PathVariable @Positive int ventaId) { return service.anular(ventaId); }
}
