package edu.comisariato.api.dto;
import java.math.BigDecimal;
import java.util.List;
import jakarta.validation.Valid;
import jakarta.validation.constraints.*;
import io.swagger.v3.oas.annotations.media.Schema;
public record VentaRequest(
    @NotBlank @Size(max=20) @Pattern(regexp="[A-Za-z0-9-]+") @Schema(example="FAC-001") String numeroFactura,
    @Positive Integer clienteId,
    @NotNull @Positive @Schema(example="1") Integer empleadoId,
    @NotBlank @Pattern(regexp="Efectivo|Tarjeta|Transferencia") @Schema(example="Efectivo") String formaPago,
    @NotEmpty @Size(max=100) List<@NotNull @Valid Detalle> detalles
) {
    public record Detalle(
        @NotNull @Positive @Schema(example="1") Integer productoId,
        @NotNull @Positive @Max(1000000) @Schema(example="2") Integer cantidad,
        @NotNull @DecimalMin("0.00") @Digits(integer=8, fraction=2) @Schema(example="0.00") BigDecimal descuento
    ) {}
}
