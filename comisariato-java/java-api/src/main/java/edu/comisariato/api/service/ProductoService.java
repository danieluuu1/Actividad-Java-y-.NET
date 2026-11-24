package edu.comisariato.api.service;
import java.util.List;
import edu.comisariato.api.dto.*;
import edu.comisariato.api.exception.NotFoundException;
import edu.comisariato.api.repository.ProductoRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
@Service
@Transactional(readOnly=true)
public class ProductoService {
    private static final Logger log = LoggerFactory.getLogger(ProductoService.class);
    private final ProductoRepository repository;
    public ProductoService(ProductoRepository repository) { this.repository=repository; }
    public List<ProductoResponse> stockBajo() {
        List<ProductoResponse> result = repository.stockBajo();
        log.info("Stock bajo consultado: resultados={}", result.size());
        return result;
    }
    public List<MovimientoResponse> movimientos(int id) {
        if (!repository.existe(id)) throw new NotFoundException("El producto no existe.");
        List<MovimientoResponse> result = repository.movimientos(id);
        log.info("Movimientos consultados: productoId={}, resultados={}", id, result.size());
        return result;
    }
}
