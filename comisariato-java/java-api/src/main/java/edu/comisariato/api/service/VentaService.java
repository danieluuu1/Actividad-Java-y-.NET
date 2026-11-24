package edu.comisariato.api.service;
import java.util.HashSet;
import java.util.List;
import edu.comisariato.api.dto.*;
import edu.comisariato.api.exception.*;
import edu.comisariato.api.repository.VentaRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.retry.annotation.Backoff;
import org.springframework.retry.annotation.Retryable;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import org.springframework.transaction.support.TransactionSynchronization;
import org.springframework.transaction.support.TransactionSynchronizationManager;
@Service
public class VentaService {
    private static final Logger log = LoggerFactory.getLogger(VentaService.class);
    private final VentaRepository repository;
    public VentaService(VentaRepository repository) { this.repository=repository; }
    @Transactional
    @Retryable(retryFor=DeadlockException.class, maxAttempts=2, backoff=@Backoff(delay=1000))
    public VentaResponse registrar(VentaRequest request) {
        var ids = new HashSet<Integer>();
        for (var item : request.detalles()) {
            if (!ids.add(item.productoId())) throw new BadRequestException("Un producto no puede repetirse en el detalle.");
        }
        VentaResponse result = repository.registrar(request);
        afterCommit("registrada", result.ventaId());
        return result;
    }
    @Transactional
    @Retryable(retryFor=DeadlockException.class, maxAttempts=2, backoff=@Backoff(delay=1000))
    public VentaResponse anular(int id) {
        VentaResponse result = repository.anular(id);
        afterCommit("anulada", id);
        return result;
    }
    @Transactional(readOnly=true)
    public List<VentaResponse> porEmpleado(int id) {
        if (!repository.existeEmpleado(id)) throw new NotFoundException("El empleado no existe.");
        List<VentaResponse> result = repository.porEmpleado(id);
        log.info("Ventas consultadas: empleadoId={}, resultados={}", id, result.size());
        return result;
    }
    private void afterCommit(String action, int id) {
        TransactionSynchronizationManager.registerSynchronization(new TransactionSynchronization() {
            @Override public void afterCommit() { log.info("Venta {} confirmada: ventaId={}", action, id); }
        });
    }
}
