package edu.comisariato.api.dto;
import java.math.BigDecimal;
public record ProductoResponse(int productoId, String codigoBarras, String nombre,
    BigDecimal precioVenta, int stock, int stockMinimo) {}
