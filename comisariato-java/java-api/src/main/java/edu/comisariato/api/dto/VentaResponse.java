package edu.comisariato.api.dto;
import java.math.BigDecimal;
import java.time.LocalDateTime;
public record VentaResponse(int ventaId, String numeroFactura, Integer clienteId,
    int empleadoId, LocalDateTime fechaVenta, BigDecimal subtotal, BigDecimal iva,
    BigDecimal total, String formaPago, String estado) {}
