package edu.comisariato.api.controller;
import java.util.List;
import jakarta.validation.constraints.Positive;
import edu.comisariato.api.dto.*;
import edu.comisariato.api.service.ProductoService;
import io.swagger.v3.oas.annotations.Operation;
import org.springframework.web.bind.annotation.*;
@RestController @RequestMapping("/api/productos")
public class ProductoController {
    private final ProductoService service;
    public ProductoController(ProductoService service) { this.service=service; }
    @Operation(summary="Productos activos con stock menor o igual al minimo")
    @GetMapping("/stock-bajo")
    public List<ProductoResponse> stockBajo() { return service.stockBajo(); }
    @Operation(summary="Movimientos por producto; 404 si no existe, 200 [] si no tiene movimientos")
    @GetMapping("/{productoId}/movimientos")
    public List<MovimientoResponse> movimientos(@PathVariable @Positive int productoId) { return service.movimientos(productoId); }
}
