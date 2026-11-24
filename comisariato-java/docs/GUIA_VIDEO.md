# Guion de demostración (6 a 8 minutos)

Este documento es un guion; la grabación debe realizarse con la API conectada a SQL Server. No contiene evidencia de una demostración ya grabada.

## Preparación

1. Instala la BD y los objetos SQL, configura las credenciales y ejecuta la API.
2. Abre Swagger, `/actuator/health`, el menú Java y SSMS.
3. Usa datos de prueba. Guarda el stock inicial de los productos que utilizarás.
4. Elige un número de factura nuevo, por ejemplo `VIDEO-2411-01` si todavía no existe.
5. Oculta contraseñas y variables de entorno al compartir pantalla.

## 00:00 a 00:45 - Presentación y arquitectura

Explica: «Esta es la aplicación Java del taller. Usa Spring Boot 3, JDBC y SQL Server. Está dividida en controller, service, repository y config. No utiliza JPA ni Hibernate». Muestra los paquetes en el IDE y las dependencias en `pom.xml`.

## 00:45 a 01:30 - Infraestructura

Muestra las tablas originales y los cuatro objetos exigidos: `usp_RegistrarVenta`, `usp_AnularVenta`, `vw_ProductosStockBajo` y `fn_VentasPorEmpleado`. Abre Swagger y verifica que Actuator responde `UP`.

## 01:30 a 03:00 - Registro y errores

Desde el menú, registra dos unidades del producto 1 con empleado 1, pago Efectivo y descuento cero. Muestra HTTP 201 y el ID real devuelto. Con los precios originales, el total es 2.53. Enseña que no se ingresó el total ni el precio.

Repite el mismo número de factura: HTTP 409. En Swagger, omite `empleadoId`: HTTP 400. Con una factura nueva pide una cantidad mayor que el stock: HTTP 422. Verifica en SSMS que el intento fallido no insertó una cabecera ni modificó stock.

## 03:00 a 04:00 - Consultas

Consulta ventas del empleado 1 y los movimientos del producto 1 desde el menú. Consulta el empleado 2: si todavía no tiene ventas, la respuesta es 200 con `[]`. Consulta un ID inexistente para demostrar 404.

Consulta stock bajo. Si devuelve `[]`, explica que es correcto porque los datos iniciales superan el mínimo. Para mostrar un resultado no vacío, en una BD de laboratorio registra una venta que deje al producto 1 en 20 unidades (cantidad = stock actual menos 20). Guarda su ID para anularla después.

## 04:00 a 05:00 - Anulación

Anula la venta creada al principio: HTTP 200. Muestra stock restituido y el movimiento de Entrada vinculado a la misma venta. Repite la anulación: HTTP 409; el stock no aumenta por segunda vez. Anula también cualquier venta adicional de la demostración.

## 05:00 a 06:30 - Transacciones y pruebas

Muestra `@Transactional`, `@Retryable(maxAttempts=2, backoff=@Backoff(delay=1000))` y el orden de los interceptores. Explica: «Si SQL Server devuelve 1205, el primer intento se revierte y se espera un segundo antes de iniciar una transacción nueva. Solo se reintenta una vez».

Ejecuta `mvnw.cmd verify`. Distingue los tests de contratos con mocks de las pruebas SQL reales, que se habilitan con `RUN_SQL_INTEGRATION=true`. No afirmes haber probado concurrencia en la BD si esos tests aparecen omitidos.

## 06:30 a 07:30 - Cierre y ejecución

Muestra las instrucciones del README: instalar scripts, compilar, configurar variables, ejecutar JAR y abrir el menú. Indica que el IVA del 15 % es una convención académica configurable y que el informe documenta las decisiones.

## Consultas útiles durante la grabación

```sql
SELECT ProductoID, Nombre, Stock, StockMinimo FROM dbo.Productos;
SELECT VentaID, NumeroFactura, Subtotal, IVA, Total, Estado FROM dbo.Ventas ORDER BY VentaID DESC;
SELECT * FROM dbo.DetalleVenta ORDER BY DetalleVentaID DESC;
SELECT * FROM dbo.MovimientosInventario ORDER BY MovimientoID DESC;
SELECT * FROM dbo.vw_ProductosStockBajo;
SELECT * FROM dbo.fn_VentasPorEmpleado(1);
```
