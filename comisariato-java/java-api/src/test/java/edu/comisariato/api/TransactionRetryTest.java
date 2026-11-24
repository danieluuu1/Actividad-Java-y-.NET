package edu.comisariato.api;
import java.math.BigDecimal;
import java.sql.SQLException;
import java.util.List;
import edu.comisariato.api.dto.VentaRequest;
import edu.comisariato.api.exception.*;
import edu.comisariato.api.repository.VentaRepository;
import edu.comisariato.api.service.VentaService;
import org.junit.jupiter.api.*;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.*;
import org.springframework.core.Ordered;
import org.springframework.retry.annotation.EnableRetry;
import org.springframework.test.context.junit.jupiter.SpringJUnitConfig;
import org.springframework.transaction.*;
import org.springframework.transaction.annotation.EnableTransactionManagement;
import org.springframework.transaction.support.*;
import static org.mockito.Mockito.*;
import static org.junit.jupiter.api.Assertions.*;
@SpringJUnitConfig(TransactionRetryTest.Config.class)
class TransactionRetryTest {
    @Configuration
    @EnableRetry(order=Ordered.LOWEST_PRECEDENCE-1)
    @EnableTransactionManagement(order=Ordered.LOWEST_PRECEDENCE)
    static class Config {
        @Bean VentaRepository repository() { return mock(VentaRepository.class); }
        @Bean VentaService service(VentaRepository repo) { return new VentaService(repo); }
        @Bean CountingTransactionManager transactionManager() { return new CountingTransactionManager(); }
    }
    static class CountingTransactionManager extends AbstractPlatformTransactionManager {
        int begins,commits,rollbacks;
        @Override protected Object doGetTransaction() { return new Object(); }
        @Override protected void doBegin(Object tx,TransactionDefinition definition) { begins++; }
        @Override protected void doCommit(DefaultTransactionStatus s) { commits++; }
        @Override protected void doRollback(DefaultTransactionStatus s) { rollbacks++; }
    }
    @Autowired VentaService service;
    @Autowired VentaRepository repo;
    @Autowired CountingTransactionManager tx;
    @BeforeEach void resetState() { reset(repo); tx.begins=tx.commits=tx.rollbacks=0; }
    static VentaRequest request() { return new VentaRequest("FAC-001",null,1,"Efectivo",List.of(new VentaRequest.Detalle(1,2,BigDecimal.ZERO))); }
    static DeadlockException deadlock() { return new DeadlockException(new SQLException("deadlock","40001",1205)); }
    @Test void deadlockRetriesInNewTransactionAfterOneSecond() {
        when(repo.registrar(any())).thenThrow(deadlock()).thenReturn(ApiContractTest.response("Completada"));
        long start=System.nanoTime();
        service.registrar(request());
        assertTrue(System.nanoTime()-start>=1_000_000_000L);
        assertEquals(2,tx.begins); assertEquals(1,tx.rollbacks); assertEquals(1,tx.commits);
        verify(repo,times(2)).registrar(any());
    }
    @Test void neverRetriesMoreThanOnce() {
        when(repo.anular(1)).thenThrow(deadlock());
        assertThrows(DeadlockException.class,()->service.anular(1));
        verify(repo,times(2)).anular(1);
        assertEquals(2,tx.rollbacks); assertEquals(0,tx.commits);
    }
    @Test void businessErrorRollsBackAndDoesNotRetry() {
        when(repo.registrar(any())).thenThrow(new BusinessException("stock"));
        assertThrows(BusinessException.class,()->service.registrar(request()));
        assertEquals(1,tx.begins); assertEquals(1,tx.rollbacks); assertEquals(0,tx.commits);
        verify(repo,times(1)).registrar(any());
    }
    @Test void duplicatesRejectedBeforeRepository() {
        var r=request();
        var duplicated=new VentaRequest(r.numeroFactura(),null,1,"Efectivo",List.of(r.detalles().get(0),r.detalles().get(0)));
        assertThrows(BadRequestException.class,()->service.registrar(duplicated));
        verifyNoInteractions(repo); assertEquals(1,tx.rollbacks);
    }
    @Test void existingEmployeeCanHaveEmptyArray() {
        when(repo.existeEmpleado(2)).thenReturn(true); when(repo.porEmpleado(2)).thenReturn(List.of());
        assertTrue(service.porEmpleado(2).isEmpty());
    }
    @Test void missingEmployeeIsNotEmptyArray() {
        when(repo.existeEmpleado(999)).thenReturn(false);
        assertThrows(NotFoundException.class,()->service.porEmpleado(999));
        verify(repo,never()).porEmpleado(anyInt());
    }
}
