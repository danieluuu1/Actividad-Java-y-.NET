package edu.comisariato.api.dto;
import java.time.LocalDateTime;
public record MovimientoResponse(long movimientoId, int productoId, String tipoMovimiento,
    int cantidad, int stockAnterior, int stockNuevo, String referenciaTipo,
    Integer referenciaId, LocalDateTime fechaMovimiento, Integer empleadoId) {}
