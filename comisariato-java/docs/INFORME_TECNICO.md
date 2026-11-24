# Informe técnico: Comisariato Java

**Taller de integración · Aplicación Java · 24 de noviembre de 2026**

## 1. Objetivo y alcance

Implementar una aplicación Java autónoma que consuma la base Comisariato de Microsoft SQL Server. Se conservan las tablas y los datos del SQL proporcionado y se incorporan los objetos de base de datos requeridos por el taller. El alcance elegido por el solicitante es Java; no se construye la aplicación .NET.

La entrega contiene API REST, menú de consola Java, SQL, pruebas, instrucciones de instalación, informe y guion de video. El estado verificable de las pruebas se registra por separado en VALIDACION.md. El guion no equivale a un video grabado y el repositorio local no implica publicación en un servicio remoto.

## 2. Tecnología y arquitectura

Se utiliza Java con bytecode objetivo 17, Spring Boot 3.5.8 y Maven. La compilación de la entrega se realiza con JDK 21. Se utiliza JdbcTemplate y el controlador oficial Microsoft mssql-jdbc 13.2.1.jre11. El sufijo jre11 identifica la variante compatible con Java 11 o superior; no significa que el proyecto use Java 11.

Las dependencias incluyen Validation, Actuator, Spring Retry, Spring AOP y springdoc-openapi 2.8.14. No se incorpora JPA, Hibernate ni un ORM. Los DTO son records y los importes monetarios se representan con BigDecimal.

El flujo es controller → service → repository, con DatabaseConfig como configuración compartida de acceso a datos. El controlador recibe solicitudes y dispara Bean Validation; el servicio controla transacciones, reglas y reintentos; el repositorio ejecuta SQL parametrizado; SQL Server conserva la lógica de cálculo y modificación atómica. DatabaseConfig crea JdbcTemplate usando el DataSource que Spring configura desde application.yml y variables de entorno.

El menú de consola utiliza HttpClient de Java y consume los mismos endpoints que Swagger. No accede directamente a la base. Su ejecución es independiente del proceso de la API y no requiere bibliotecas adicionales.

## 3. Matriz de cumplimiento Java

| Requisito del taller | Implementación |
|---|---|
| Java 17 LTS o superior | Objetivo Java 17 en pom.xml; JDK 21 para compilar |
| Spring Boot 3.x | Spring Boot 3.5.8 |
| Maven o Gradle | Maven y Maven Wrapper |
| JDBC, sin JPA/Hibernate | JdbcTemplate y SQL explícito |
| Driver oficial | com.microsoft.sqlserver:mssql-jdbc |
| Capas obligatorias | controller, service, repository, config |
| DTO record | VentaRequest, VentaResponse, ProductoResponse, MovimientoResponse, ErrorResponse |
| Validación | @NotNull, @NotBlank, @Positive, @Size, @Max, @Digits, @DecimalMin, @Pattern y @Valid |
| Excepciones personalizadas | NotFoundException, ConflictException, BusinessException y BadRequestException |
| Manejo global | @RestControllerAdvice en GlobalExceptionHandler |
| Transacción y rollback | @Transactional en servicios; errores runtime |
| Configuración externa | DB_URL, DB_USER, DB_PASSWORD y application.yml.example |
| Swagger | /swagger-ui.html y /v3/api-docs |
| Logging | SLF4J; INFO después del commit y ERROR con contexto y stacktrace |
| Health | /actuator/health, incluye conectividad de DataSource |
| Deadlock 1205 | @Retryable, dos intentos totales, espera de 1000 ms |
| Menú de cinco operaciones | MenuConsola.java |

## 4. Contratos HTTP

### Registrar venta

POST /api/ventas recibe numeroFactura, clienteId opcional, empleadoId, formaPago y un array detalles. Cada detalle contiene productoId, cantidad y descuento. El descuento corresponde al importe de la línea completa. El request no incluye precios ni importes calculados.

Los campos desconocidos se rechazan, evitando aceptar silenciosamente un total enviado por el cliente. Las cantidades deben ser enteras positivas, los IDs positivos, la factura de 1 a 20 caracteres alfanuméricos o guiones y el pago uno de los tres valores del esquema. Se valida también el contenido de cada elemento del array y se rechazan elementos nulos y productos repetidos.

Respuestas: 201 al crear; 400 por entrada inválida; 404 por cliente, empleado o producto inexistente; 409 por factura duplicada; 422 por stock insuficiente, producto/empleado inactivo, descuento superior al importe o desbordamiento del esquema monetario. El cuerpo exitoso incluye ventaId, numeroFactura, clienteId, empleadoId, fechaVenta, subtotal, iva, total, formaPago y estado.

### Anular venta

POST /api/ventas/{ventaId}/anular no necesita cuerpo. Responde 200 cuando anula; 404 si no existe; 409 si ya está anulada o su estado no es Completada. Conserva cabecera y detalle, cambia Estado a Anulada, restituye stock y agrega movimientos de Entrada. Una repetición no duplica la devolución de existencias.

### Consultas

GET /api/productos/stock-bajo devuelve los productos activos cuyo Stock sea menor o igual a StockMinimo. GET /api/empleados/{empleadoId}/ventas distingue un empleado inexistente (404) de uno sin ventas (200 con array vacío). La función incluye ventas anuladas, identificadas por Estado. GET /api/productos/{productoId}/movimientos aplica la misma distinción de existencia y devuelve el historial ordenado del más reciente al más antiguo.

Los identificadores de ruta inválidos devuelven 400. Los errores inesperados devuelven 500 con un mensaje público genérico. El log conserva la excepción y el contexto. El formato de error incluye timestamp, status, codigo, mensaje, ruta y errores de campos.

## 5. Objetos agregados a SQL Server

### dbo.usp_RegistrarVenta

Parámetros: @NumeroFactura VARCHAR(20), @ClienteID INT opcional, @EmpleadoID INT, @FormaPago NVARCHAR(30), @Detalles NVARCHAR(MAX). Los detalles se transfieren como JSON mediante un parámetro JDBC y se procesan con OPENJSON; se requiere compatibilidad de base de datos 130 o superior.

El procedimiento valida la entrada y la existencia/estado de los recursos. Protege la factura con un bloqueo de rango y la restricción UNIQUE original. Recorre productos en orden de ProductoID usando UPDLOCK y HOLDLOCK, retiene los bloqueos hasta el final de la transacción y verifica el stock. Los precios se leen de Productos, nunca del cliente.

La fórmula por línea es cantidad × precioVenta − descuento. El subtotal es la suma de líneas; el IVA se redondea a dos decimales y el total es subtotal + IVA. Se valida que los importes quepan en DECIMAL(10,2). Luego se inserta la cabecera, el detalle, se descuenta stock y se registra cada Salida. El ID se obtiene con SCOPE_IDENTITY.

### dbo.usp_AnularVenta

Recibe @VentaID INT. Bloquea la cabecera para impedir dos anulaciones concurrentes. Solo procesa ventas completadas con detalle. Recorre productos en el mismo orden global usado por el registro, valida capacidad de INT, restituye stock, registra Entrada y cambia el estado. Los precios y totales históricos no se recalculan.

### Vista, función y configuración

vw_ProductosStockBajo filtra activos y aplica Stock <= StockMinimo. fn_VentasPorEmpleado es una función de tabla en línea que recibe un empleado y devuelve sus ventas. La comprobación de existencia del empleado se realiza en el servicio antes de consultarla.

ConfiguracionFacturacion contiene una única fila y una tasa entre 0 y 1. Se inicializa en 0.1500 como supuesto académico. El taller no define la política de IVA, exenciones ni reglas tributarias por producto; el 15 % no constituye una determinación de cumplimiento tributario. La tasa puede cambiarse en SQL; las ventas ya registradas conservan sus importes.

```sql
UPDATE dbo.ConfiguracionFacturacion SET TasaIVA=0.1500 WHERE ConfiguracionID=1;
```

## 6. Transacciones, concurrencia y reintento

El servicio abre la transacción con @Transactional. JdbcTemplate participa en esa misma conexión transaccional. Un retorno normal se confirma y cualquier RuntimeException provoca rollback. Los SP detectan si ya existe una transacción: cuando Java la abrió, no ejecutan un commit independiente; cuando se invocan directamente desde SSMS sin transacción, administran una propia. SET XACT_ABORT ON evita continuar una operación parcialmente fallida.

Los bloqueos mantienen estable el stock mientras se comprueba y descuenta. La anulación retiene el bloqueo de la venta hasta confirmar, por lo que solo una solicitud puede devolver el stock. El orden ascendente de productos reduce el riesgo de deadlock; no promete eliminarlo completamente.

SqlErrorTranslator inspecciona SQLException y sus excepciones encadenadas. Traduce 51000 a entrada inválida, 51004 a no encontrado, 51009 a conflicto y 51022 a regla de negocio. Los errores UNIQUE 2601/2627 se consideran conflicto y el 1205 se convierte en DeadlockException. Los errores no reconocidos permanecen como errores internos.

El interceptor de reintento tiene prioridad externa al interceptor de transacción. Se ejecutan como máximo dos intentos totales con una espera de un segundo. Así, el primer intento termina con rollback antes del segundo. No se reintenta dentro de una transacción ya abortada. Un segundo deadlock devuelve 409. Los logs de éxito se emiten mediante afterCommit, de modo que no anuncian una venta confirmada antes de que el commit termine.

## 7. Revisión del SQL original

El esquema original contiene PK, FK, índices de apoyo, restricciones UNIQUE, CHECK, valores predeterminados, importes DECIMAL y subtotales calculados persistidos. Estas son bases útiles para integridad relacional. El número de factura único y el detalle único por venta/producto apoyan los contratos del taller.

El archivo original no contiene los procedimientos, la vista ni la función exigidos. Por sí solo tampoco mantiene el stock al insertar un detalle, calcula los totales de la cabecera, impide una doble anulación ni registra movimientos automáticamente. La solución cubre esos flujos a través de los SP; el rol de aplicación no tiene escritura directa en tablas.

Otras limitaciones identificadas: no se comprueba en el esquema que Total = Subtotal + IVA ni que la cabecera corresponda a la suma de detalles; las referencias polimórficas de MovimientosInventario no tienen FK; Ajuste no exige una relación matemática entre cantidades y stocks; las cédulas/RUC tienen unicidad pero no validación de dígitos; los datos iniciales de stock no tienen movimientos de apertura; no se conserva una tasa de impuesto histórica explícita; DATETIME2 no guarda zona horaria; la instalación inicial no es repetible ni transaccional de principio a fin.

No se cambian silenciosamente esas decisiones en las tablas originales. Los SP garantizan los flujos implementados, pero una escritura de administrador o un programa externo que ignore esos procedimientos puede romper invariantes. La auditoría de la anulación usa EmpleadoID de la venta original porque el contrato no incluye un usuario anulador ni autenticación; esto no equivale a identificar al operador real que solicitó la anulación.

## 8. Seguridad y configuración

Se usan parámetros SQL y no se concatenan datos del request en consultas. No se versionan credenciales. El rol comisariato_app permite ejecutar los SP y consultar los objetos utilizados, sin INSERT, UPDATE o DELETE directos. Los recursos JDBC son gestionados por Spring.

La API se enlaza a loopback por defecto. El taller no pide autenticación ni autorización HTTP, por lo que no se implementan usuarios ni JWT. Antes de una exposición pública serían necesarias autenticación, permisos por operación, TLS, límites de solicitudes, paginación y políticas de auditoría. Estas medidas quedan fuera del alcance académico actual.

El certificado SQL se verifica en application.yml. El script local permite confiar en el certificado autofirmado del laboratorio. El health check no expone detalles internos ni credenciales. Los errores HTTP inesperados no devuelven consultas, rutas del servidor ni stacktraces al cliente.

## 9. Estrategia de pruebas

ApiContractTest verifica controladores y validación con MockMvc y servicios simulados. Incluye creación, campos faltantes/desconocidos, cantidades inválidas, IDs inválidos, 404/409/422, arrays vacíos y error interno sin detalles sensibles.

TransactionRetryTest ejecuta los proxies reales de Spring sobre un repositorio simulado y un gestor transaccional de prueba. Comprueba dos inicios de transacción, un rollback y un commit ante el primer deadlock, la espera de un segundo, el máximo de un reintento y la ausencia de reintento para errores de negocio. Esto verifica el orden de interceptores, no una reversión real del motor SQL.

SqlErrorTranslatorTest verifica los códigos de SQL Server, incluidos los errores encadenados. SqlServerIntegrationTest requiere una base de pruebas real y comprueba venta, totales, detalle, stock, rollback, anulación, movimientos y concurrencia. Está deshabilitado por defecto para evitar modificar una base equivocada y exige un nombre ComisariatoTest.

La revisión de sintaxis T-SQL utiliza Microsoft ScriptDOM. Un análisis sintáctico correcto no demuestra ejecución correcta, resolución de objetos o comportamiento transaccional. VALIDACION.md distingue esas evidencias y registra las verificaciones pendientes.

## 10. Instalación y operación

El orden es: instalar el esquema original si hace falta; instalar los objetos API; configurar el usuario; compilar con Maven; definir DB_URL, DB_USER y DB_PASSWORD; ejecutar el JAR; verificar health y abrir el menú en otra terminal. README.md contiene comandos completos, ejemplos JSON, configuración de SQL Server y solución de problemas.

La ejecución de consultas devuelve datos reales de SQL Server. Si no existe conexión, Swagger puede seguir mostrando el contrato, pero las operaciones de negocio fallan y Actuator refleja el problema. No se incluye una base en memoria para ocultar la falta de conexión ni datos simulados en la aplicación de producción.

## 11. Referencias

- Enunciado proporcionado: Taller 24-11 grupal.pdf, secciones 2, 3, 4 y 7 y tabla de objetos SQL de la primera página.
- Esquema proporcionado: Comisariato_REVISEN_BIEN.sql, copiado íntegramente como 01_base_original.sql.
- Spring Boot 3.5: https://docs.spring.io/spring-boot/3.5/system-requirements.html
- Springdoc OpenAPI para Spring Boot 3: https://springdoc.org/v2/
- Microsoft JDBC: https://learn.microsoft.com/en-us/sql/connect/jdbc/release-notes-for-the-jdbc-driver

Las decisiones adicionales (tasa académica, definición de stock bajo, límites del request, rechazo de campos desconocidos y estado anulable) se documentan como decisiones de implementación, no como requisitos textuales del enunciado.
