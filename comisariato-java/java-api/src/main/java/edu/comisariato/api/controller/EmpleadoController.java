package edu.comisariato.api.controller;
import java.util.List;
import jakarta.validation.constraints.Positive;
import edu.comisariato.api.dto.VentaResponse;
import edu.comisariato.api.service.VentaService;
import io.swagger.v3.oas.annotations.Operation;
import org.springframework.web.bind.annotation.*;
@RestController @RequestMapping("/api/empleados")
public class EmpleadoController {
    private final VentaService service;
    public EmpleadoController(VentaService service) { this.service=service; }
    @Operation(summary="Ventas por empleado; 404 si no existe, 200 [] si existe sin ventas")
    @GetMapping("/{empleadoId}/ventas")
    public List<VentaResponse> ventas(@PathVariable @Positive int empleadoId) { return service.porEmpleado(empleadoId); }
}
