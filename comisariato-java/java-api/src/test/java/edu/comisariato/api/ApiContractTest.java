package edu.comisariato.api;
import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.List;
import edu.comisariato.api.controller.*;
import edu.comisariato.api.dto.*;
import edu.comisariato.api.exception.*;
import edu.comisariato.api.service.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.WebMvcTest;
import org.springframework.test.context.bean.override.mockito.MockitoBean;
import org.springframework.test.web.servlet.MockMvc;
import static org.mockito.ArgumentMatchers.*;
import static org.mockito.Mockito.*;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.*;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.*;
@WebMvcTest({VentaController.class,ProductoController.class,EmpleadoController.class})
class ApiContractTest {
    @Autowired MockMvc mvc;
    @MockitoBean VentaService ventas;
    @MockitoBean ProductoService productos;
    static final String VALID = """
        {"numeroFactura":"FAC-001","clienteId":null,"empleadoId":1,"formaPago":"Efectivo",
         "detalles":[{"productoId":1,"cantidad":2,"descuento":0}]}
        """;
    static VentaResponse response(String estado) {
        return new VentaResponse(1,"FAC-001",null,1,LocalDateTime.of(2026,11,24,10,0),
            new BigDecimal("2.20"),new BigDecimal("0.33"),new BigDecimal("2.53"),"Efectivo",estado);
    }
    @Test void created() throws Exception {
        when(ventas.registrar(any())).thenReturn(response("Completada"));
        mvc.perform(post("/api/ventas").contentType("application/json").content(VALID))
            .andExpect(status().isCreated()).andExpect(jsonPath("$.total").value(2.53));
    }
    @Test void wrongHttpMethod() throws Exception {
        mvc.perform(get("/api/ventas")).andExpect(status().isMethodNotAllowed());
    }
    @Test void wrongContentType() throws Exception {
        mvc.perform(post("/api/ventas").contentType("text/plain").content(VALID)).andExpect(status().isUnsupportedMediaType());
    }
    @ParameterizedTest @ValueSource(strings={
        "{}", "null", "{", "[]",
        "{\"numeroFactura\":\"FAC\",\"empleadoId\":1,\"formaPago\":\"Efectivo\",\"detalles\":[]}",
        "{\"numeroFactura\":\"FAC\",\"empleadoId\":1,\"formaPago\":\"Efectivo\",\"detalles\":[null]}"
    })
    void badBody(String body) throws Exception {
        mvc.perform(post("/api/ventas").contentType("application/json").content(body)).andExpect(status().isBadRequest());
        verifyNoInteractions(ventas);
    }
    @ParameterizedTest @ValueSource(strings={"total","iva","precioUnitario"})
    void rejectsClientCalculatedFields(String key) throws Exception {
        String body = VALID.replaceFirst("\\{", "{\""+key+"\":10,");
        mvc.perform(post("/api/ventas").contentType("application/json").content(body)).andExpect(status().isBadRequest());
        verifyNoInteractions(ventas);
    }
    @ParameterizedTest @ValueSource(strings={"0","-1","1.5","\"2\"","null"})
    void invalidQuantity(String quantity) throws Exception {
        mvc.perform(post("/api/ventas").contentType("application/json").content(VALID.replace("\"cantidad\":2","\"cantidad\":"+quantity)))
            .andExpect(status().isBadRequest());
        verifyNoInteractions(ventas);
    }
    @Test void duplicateInvoice() throws Exception { error(new ConflictException("Factura duplicada"),409); }
    @Test void noStock() throws Exception { error(new BusinessException("Stock insuficiente"),422); }
    @Test void absentEmployeeOnSale() throws Exception { error(new NotFoundException("Empleado no existe"),404); }
    @Test void deadlockExhausted() throws Exception { error(new DeadlockException(new RuntimeException()),409); }
    @Test void unexpectedErrorDoesNotExposeSql() throws Exception {
        when(ventas.registrar(any())).thenThrow(new RuntimeException("secret-password SQL SELECT"));
        mvc.perform(post("/api/ventas").contentType("application/json").content(VALID)).andExpect(status().isInternalServerError())
            .andExpect(jsonPath("$.mensaje").value("Error interno. Consulte el registro del servidor."));
    }
    void error(RuntimeException e,int status) throws Exception {
        when(ventas.registrar(any())).thenThrow(e);
        mvc.perform(post("/api/ventas").contentType("application/json").content(VALID)).andExpect(status().is(status));
    }
    @Test void cancel() throws Exception {
        when(ventas.anular(1)).thenReturn(response("Anulada"));
        mvc.perform(post("/api/ventas/1/anular")).andExpect(status().isOk()).andExpect(jsonPath("$.estado").value("Anulada"));
    }
    @Test void cancelMissing() throws Exception {
        when(ventas.anular(1)).thenThrow(new NotFoundException("No existe"));
        mvc.perform(post("/api/ventas/1/anular")).andExpect(status().isNotFound());
    }
    @Test void cancelTwice() throws Exception {
        when(ventas.anular(1)).thenThrow(new ConflictException("Anulada"));
        mvc.perform(post("/api/ventas/1/anular")).andExpect(status().isConflict());
    }
    @Test void emptyEmployeeSales() throws Exception {
        when(ventas.porEmpleado(1)).thenReturn(List.of());
        mvc.perform(get("/api/empleados/1/ventas")).andExpect(status().isOk()).andExpect(content().json("[]"));
    }
    @Test void missingEmployee() throws Exception {
        when(ventas.porEmpleado(999)).thenThrow(new NotFoundException("No existe"));
        mvc.perform(get("/api/empleados/999/ventas")).andExpect(status().isNotFound());
    }
    @Test void lowStock() throws Exception {
        when(productos.stockBajo()).thenReturn(List.of());
        mvc.perform(get("/api/productos/stock-bajo")).andExpect(status().isOk()).andExpect(content().json("[]"));
    }
    @Test void movements() throws Exception {
        when(productos.movimientos(1)).thenReturn(List.of());
        mvc.perform(get("/api/productos/1/movimientos")).andExpect(status().isOk()).andExpect(content().json("[]"));
    }
    @Test void missingProduct() throws Exception {
        when(productos.movimientos(999)).thenThrow(new NotFoundException("No existe"));
        mvc.perform(get("/api/productos/999/movimientos")).andExpect(status().isNotFound());
    }
    @ParameterizedTest @ValueSource(strings={"0","-1","abc","2147483648"})
    void invalidId(String id) throws Exception {
        mvc.perform(post("/api/ventas/"+id+"/anular")).andExpect(status().isBadRequest());
    }
}
