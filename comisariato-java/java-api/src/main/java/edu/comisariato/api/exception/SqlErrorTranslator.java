package edu.comisariato.api.exception;
import java.sql.SQLException;
import org.springframework.dao.DataAccessException;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
public final class SqlErrorTranslator {
    private static final Logger log = LoggerFactory.getLogger(SqlErrorTranslator.class);
    private SqlErrorTranslator() {}
    public static RuntimeException translate(DataAccessException error, String operation) {
        log.error("Fallo SQL en {}", operation, error);
        for (Throwable cause = error; cause != null; cause = cause.getCause()) {
            if (cause instanceof SQLException sql) {
                for (SQLException e = sql; e != null; e = e.getNextException()) {
                    switch (e.getErrorCode()) {
                        case 1205: return new DeadlockException(error);
                        case 51000: return new BadRequestException(e.getMessage());
                        case 51004: return new NotFoundException(e.getMessage());
                        case 51009: return new ConflictException(e.getMessage());
                        case 51022: return new BusinessException(e.getMessage());
                        case 2601, 2627: return new ConflictException("El numero de factura ya existe.");
                        default: break;
                    }
                }
            }
        }
        return error;
    }
}
