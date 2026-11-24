package edu.comisariato.api.dto;
import java.time.Instant;
import java.util.Map;
public record ErrorResponse(Instant timestamp, int status, String codigo, String mensaje,
    String ruta, Map<String,String> errores) {}
