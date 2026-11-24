package edu.comisariato.api.repository;
import java.sql.ResultSet;
import java.sql.SQLException;
import java.util.List;
import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.databind.ObjectMapper;
import edu.comisariato.api.dto.*;
import edu.comisariato.api.exception.SqlErrorTranslator;
import org.springframework.dao.DataAccessException;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Repository;
@Repository
public class VentaRepository {
    private final JdbcTemplate jdbc;
    private final ObjectMapper json;
    public VentaRepository(JdbcTemplate jdbc, ObjectMapper json) { this.jdbc=jdbc; this.json=json; }
    public VentaResponse registrar(VentaRequest request) {
        try {
            String details = json.writeValueAsString(request.detalles());
            return jdbc.queryForObject("EXEC dbo.usp_RegistrarVenta @NumeroFactura=?, @ClienteID=?, @EmpleadoID=?, @FormaPago=?, @Detalles=?",
                VentaRepository::map, request.numeroFactura(), request.clienteId(), request.empleadoId(), request.formaPago(), details);
        } catch (DataAccessException e) { throw SqlErrorTranslator.translate(e, "registrar venta"); }
        catch (JsonProcessingException e) { throw new IllegalStateException("No se pudo serializar el detalle", e); }
    }
    public VentaResponse anular(int id) {
        try { return jdbc.queryForObject("EXEC dbo.usp_AnularVenta @VentaID=?", VentaRepository::map, id); }
        catch (DataAccessException e) { throw SqlErrorTranslator.translate(e, "anular venta " + id); }
    }
    public boolean existeEmpleado(int id) {
        return Boolean.TRUE.equals(jdbc.queryForObject("SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Empleados WHERE EmpleadoID=?) THEN 1 ELSE 0 END", Boolean.class, id));
    }
    public List<VentaResponse> porEmpleado(int id) {
        return jdbc.query("SELECT VentaID, NumeroFactura, ClienteID, EmpleadoID, FechaVenta, Subtotal, IVA, Total, FormaPago, Estado FROM dbo.fn_VentasPorEmpleado(?) ORDER BY FechaVenta DESC, VentaID DESC", VentaRepository::map, id);
    }
    private static VentaResponse map(ResultSet rs, int row) throws SQLException {
        return new VentaResponse(rs.getInt("VentaID"), rs.getString("NumeroFactura"), rs.getObject("ClienteID", Integer.class),
            rs.getInt("EmpleadoID"), rs.getTimestamp("FechaVenta").toLocalDateTime(), rs.getBigDecimal("Subtotal"),
            rs.getBigDecimal("IVA"), rs.getBigDecimal("Total"), rs.getString("FormaPago"), rs.getString("Estado"));
    }
}
