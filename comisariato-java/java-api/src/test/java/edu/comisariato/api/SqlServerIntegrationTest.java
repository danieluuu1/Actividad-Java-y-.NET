package edu.comisariato.api;

import java.math.BigDecimal;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.*;
import edu.comisariato.api.dto.*;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.condition.EnabledIfEnvironmentVariable;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.web.client.TestRestTemplate;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.http.ResponseEntity;
import static org.junit.jupiter.api.Assertions.*;

/** Pruebas reales optativas: requieren una BD ComisariatoTest... preparada con scripts 01 y 02. */
@EnabledIfEnvironmentVariable(named="RUN_SQL_INTEGRATION",matches="true")
@SpringBootTest(webEnvironment=SpringBootTest.WebEnvironment.RANDOM_PORT)
@TestInstance(TestInstance.Lifecycle.PER_CLASS)
class SqlServerIntegrationTest {
    @Autowired TestRestTemplate http;
    @Autowired JdbcTemplate jdbc;
    int employee, product, secondProduct, limitedProduct;
    String suffix;
    @BeforeAll void prepare() {
        String db=jdbc.queryForObject("SELECT DB_NAME()",String.class);
        assertNotNull(db);
        if (!db.startsWith("ComisariatoTest")) throw new IllegalStateException("Use una BD de pruebas cuyo nombre comience con ComisariatoTest");
        suffix=UUID.randomUUID().toString().replace("-","").substring(0,10);
        int category=jdbc.queryForObject("INSERT dbo.Categorias(Nombre) OUTPUT INSERTED.CategoriaID VALUES(?)",Integer.class,"Prueba-"+suffix);
        employee=jdbc.queryForObject("INSERT dbo.Empleados(Cedula,Nombres,Apellidos,Cargo,FechaIngreso,Salario) OUTPUT INSERTED.EmpleadoID VALUES(?,N'Prueba',N'Integracion',N'Cajero','2026-01-01',500)",Integer.class,suffix);
        product=product(category,"A",10);
        secondProduct=product(category,"B",0);
        limitedProduct=product(category,"C",1);
    }
    int product(int category,String code,int stock) {
        return jdbc.queryForObject("INSERT dbo.Productos(CodigoBarras,Nombre,CategoriaID,PrecioCompra,PrecioVenta,Stock,StockMinimo) OUTPUT INSERTED.ProductoID VALUES(?,?,?,1,2,?,3)",Integer.class,suffix+code,"Prueba "+code,category,stock);
    }
    VentaRequest request(String invoice,int id,int quantity) {
        return new VentaRequest(invoice,null,employee,"Efectivo",List.of(new VentaRequest.Detalle(id,quantity,BigDecimal.ZERO)));
    }
    int stock(int id) { return jdbc.queryForObject("SELECT Stock FROM dbo.Productos WHERE ProductoID=?",Integer.class,id); }
    int count(String table,String invoice) {
        // table solo proviene de constantes de esta prueba, nunca de un request HTTP.
        return jdbc.queryForObject("SELECT COUNT(*) FROM dbo."+table+" WHERE VentaID IN (SELECT VentaID FROM dbo.Ventas WHERE NumeroFactura=?)",Integer.class,invoice);
    }
    @Test void lifecycleAndRollback() {
        String number="IT-"+suffix;
        var r=http.postForEntity("/api/ventas",request(number,product,2),VentaResponse.class);
        assertEquals(201,r.getStatusCode().value());
        var sale=r.getBody(); assertNotNull(sale);
        assertEquals(0,new BigDecimal("4.00").compareTo(sale.subtotal()));
        BigDecimal rate=jdbc.queryForObject("SELECT TasaIVA FROM dbo.ConfiguracionFacturacion WHERE ConfiguracionID=1",BigDecimal.class);
        BigDecimal expectedIva=new BigDecimal("4.00").multiply(rate).setScale(2,java.math.RoundingMode.HALF_UP);
        assertEquals(0,expectedIva.compareTo(sale.iva()));
        assertEquals(0,sale.subtotal().add(sale.iva()).compareTo(sale.total()));
        assertEquals(8,stock(product)); assertEquals(1,count("DetalleVenta",number));
        assertEquals(409,http.postForEntity("/api/ventas",request(number,product,2),String.class).getStatusCode().value());
        assertEquals(8,stock(product));

        var bad=new VentaRequest("FAIL-"+suffix,null,employee,"Efectivo",List.of(
            new VentaRequest.Detalle(product,1,BigDecimal.ZERO),new VentaRequest.Detalle(secondProduct,1,BigDecimal.ZERO)));
        int before=jdbc.queryForObject("SELECT COUNT(*) FROM dbo.MovimientosInventario WHERE ProductoID=?",Integer.class,product);
        assertEquals(422,http.postForEntity("/api/ventas",bad,String.class).getStatusCode().value());
        assertEquals(8,stock(product));
        assertEquals(0,jdbc.queryForObject("SELECT COUNT(*) FROM dbo.Ventas WHERE NumeroFactura=?",Integer.class,bad.numeroFactura()));
        assertEquals(before,jdbc.queryForObject("SELECT COUNT(*) FROM dbo.MovimientosInventario WHERE ProductoID=?",Integer.class,product));

        String cancel="/api/ventas/"+sale.ventaId()+"/anular";
        assertEquals(200,http.postForEntity(cancel,null,String.class).getStatusCode().value());
        assertEquals(10,stock(product));
        assertEquals(409,http.postForEntity(cancel,null,String.class).getStatusCode().value());
        assertEquals(10,stock(product));
        assertEquals(2,jdbc.queryForObject("SELECT COUNT(*) FROM dbo.MovimientosInventario WHERE ReferenciaTipo=N'Venta' AND ReferenciaID=?",Integer.class,sale.ventaId()));
        assertEquals(404,http.postForEntity("/api/ventas/2147483647/anular",null,String.class).getStatusCode().value());
        assertEquals(404,http.getForEntity("/api/empleados/2147483647/ventas",String.class).getStatusCode().value());
        assertEquals(200,http.getForEntity("/api/productos/stock-bajo",String.class).getStatusCode().value());
        assertEquals(200,http.getForEntity("/api/productos/"+product+"/movimientos",String.class).getStatusCode().value());
    }
    @Test void concurrentSalesCannotOversell() throws Exception {
        ExecutorService pool=Executors.newFixedThreadPool(2);
        CountDownLatch ready=new CountDownLatch(2), start=new CountDownLatch(1);
        try {
            Callable<ResponseEntity<String>> a=()->{ready.countDown();start.await();return http.postForEntity("/api/ventas",request("CA-"+suffix,limitedProduct,1),String.class);};
            Callable<ResponseEntity<String>> b=()->{ready.countDown();start.await();return http.postForEntity("/api/ventas",request("CB-"+suffix,limitedProduct,1),String.class);};
            var fa=pool.submit(a); var fb=pool.submit(b);
            assertTrue(ready.await(5,TimeUnit.SECONDS)); start.countDown();
            var codes=List.of(fa.get(90,TimeUnit.SECONDS).getStatusCode().value(),fb.get(90,TimeUnit.SECONDS).getStatusCode().value());
            assertTrue(codes.contains(201)); assertTrue(codes.contains(422)); assertEquals(0,stock(limitedProduct));
        } finally { start.countDown(); pool.shutdownNow(); }
    }
}
