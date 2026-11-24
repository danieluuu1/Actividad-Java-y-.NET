package edu.comisariato.api.repository;
import java.util.List;
import edu.comisariato.api.dto.*;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Repository;
@Repository
public class ProductoRepository {
    private final JdbcTemplate jdbc;
    public ProductoRepository(JdbcTemplate jdbc) { this.jdbc=jdbc; }
    public List<ProductoResponse> stockBajo() {
        return jdbc.query("SELECT ProductoID, CodigoBarras, Nombre, PrecioVenta, Stock, StockMinimo FROM dbo.vw_ProductosStockBajo ORDER BY Stock, ProductoID",
            (r,n) -> new ProductoResponse(r.getInt("ProductoID"),r.getString("CodigoBarras"),r.getString("Nombre"),r.getBigDecimal("PrecioVenta"),r.getInt("Stock"),r.getInt("StockMinimo")));
    }
    public boolean existe(int id) {
        return Boolean.TRUE.equals(jdbc.queryForObject("SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Productos WHERE ProductoID=?) THEN 1 ELSE 0 END",Boolean.class,id));
    }
    public List<MovimientoResponse> movimientos(int id) {
        return jdbc.query("SELECT MovimientoID, ProductoID, TipoMovimiento, Cantidad, StockAnterior, StockNuevo, ReferenciaTipo, ReferenciaID, FechaMovimiento, EmpleadoID FROM dbo.MovimientosInventario WHERE ProductoID=? ORDER BY FechaMovimiento DESC, MovimientoID DESC",
            (r,n) -> new MovimientoResponse(r.getLong("MovimientoID"),r.getInt("ProductoID"),r.getString("TipoMovimiento"),
                r.getInt("Cantidad"),r.getInt("StockAnterior"),r.getInt("StockNuevo"),r.getString("ReferenciaTipo"),
                r.getObject("ReferenciaID",Integer.class),r.getTimestamp("FechaMovimiento").toLocalDateTime(),r.getObject("EmpleadoID",Integer.class)),id);
    }
}
