package edu.comisariato.api.exception;
import java.time.Instant;
import java.util.LinkedHashMap;
import java.util.Map;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.validation.ConstraintViolationException;
import edu.comisariato.api.dto.ErrorResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.ResponseEntity;
import org.springframework.http.converter.HttpMessageNotReadableException;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.RestControllerAdvice;
import org.springframework.web.method.annotation.HandlerMethodValidationException;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;
import org.springframework.web.ErrorResponseException;
@RestControllerAdvice
public class GlobalExceptionHandler {
    private static final Logger log = LoggerFactory.getLogger(GlobalExceptionHandler.class);
    @ExceptionHandler(BusinessException.class)
    public ResponseEntity<ErrorResponse> business(BusinessException e, HttpServletRequest request) {
        int status = e instanceof NotFoundException ? 404 : e instanceof ConflictException ? 409
            : e instanceof BadRequestException ? 400 : 422;
        return response(e, request, status, e.getMessage(), Map.of());
    }
    @ExceptionHandler(MethodArgumentNotValidException.class)
    public ResponseEntity<ErrorResponse> validation(MethodArgumentNotValidException e, HttpServletRequest request) {
        Map<String,String> fields = new LinkedHashMap<>();
        e.getBindingResult().getFieldErrors().forEach(f -> fields.put(f.getField(), f.getDefaultMessage()));
        return response(e, request, 400, "Revise los campos del request.", fields);
    }
    @ExceptionHandler({HttpMessageNotReadableException.class, MethodArgumentTypeMismatchException.class,
        ConstraintViolationException.class, HandlerMethodValidationException.class})
    public ResponseEntity<ErrorResponse> invalid(Exception e, HttpServletRequest request) {
        return response(e, request, 400, "JSON o parametros invalidos. No envie total, IVA ni precios; use los campos documentados.", Map.of());
    }
    @ExceptionHandler(DeadlockException.class)
    public ResponseEntity<ErrorResponse> deadlock(DeadlockException e, HttpServletRequest request) {
        return response(e, request, 409, "Conflicto de concurrencia despues de dos intentos. Vuelva a intentar.", Map.of());
    }
    @ExceptionHandler(ErrorResponseException.class)
    public ResponseEntity<ErrorResponse> http(ErrorResponseException e, HttpServletRequest request) {
        return response(e, request, e.getStatusCode().value(), "Solicitud HTTP no admitida.", Map.of());
    }
    @ExceptionHandler(Exception.class)
    public ResponseEntity<ErrorResponse> unexpected(Exception e, HttpServletRequest request) {
        if (e instanceof org.springframework.web.ErrorResponse httpError) {
            return response(e, request, httpError.getStatusCode().value(), "Solicitud HTTP no admitida.", Map.of());
        }
        return response(e, request, 500, "Error interno. Consulte el registro del servidor.", Map.of());
    }
    private ResponseEntity<ErrorResponse> response(Exception e, HttpServletRequest r, int status,
            String message, Map<String,String> fields) {
        log.error("{} {} -> HTTP {}", r.getMethod(), r.getRequestURI(), status, e);
        String code = switch(status) {
            case 400 -> "VALIDACION"; case 404 -> "NO_ENCONTRADO"; case 409 -> "CONFLICTO";
            case 422 -> "REGLA_NEGOCIO"; default -> "ERROR_HTTP_" + status;
        };
        return ResponseEntity.status(status).body(new ErrorResponse(Instant.now(), status, code, message, r.getRequestURI(), fields));
    }
}
