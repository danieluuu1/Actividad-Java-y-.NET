package edu.comisariato.api;
import java.sql.SQLException;
import edu.comisariato.api.exception.*;
import org.junit.jupiter.api.Test;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.UncategorizedSQLException;
import static org.junit.jupiter.api.Assertions.*;
class SqlErrorTranslatorTest {
    RuntimeException translate(int code) {
        return SqlErrorTranslator.translate(new UncategorizedSQLException("sp","EXEC",new SQLException("test","",code)),"test");
    }
    @Test void mapsSqlServerCodes() {
        assertInstanceOf(BadRequestException.class,translate(51000));
        assertInstanceOf(NotFoundException.class,translate(51004));
        assertInstanceOf(ConflictException.class,translate(51009));
        assertInstanceOf(ConflictException.class,translate(2627));
        assertInstanceOf(ConflictException.class,translate(2601));
        assertInstanceOf(BusinessException.class,translate(51022));
        assertInstanceOf(DeadlockException.class,translate(1205));
        assertInstanceOf(DataAccessException.class,translate(18456));
    }
    @Test void followsNextException() {
        SQLException root=new SQLException("outer"); root.setNextException(new SQLException("deadlock","40001",1205));
        assertInstanceOf(DeadlockException.class,SqlErrorTranslator.translate(new UncategorizedSQLException("sp","EXEC",root),"test"));
    }
}
