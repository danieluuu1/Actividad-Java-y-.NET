# Comisariato: aplicación Java del taller

API REST con **Java 17+, Spring Boot 3.5.8, Maven, Spring JDBC y SQL Server**. Incluye las cinco operaciones del taller, un menú de consola escrito en Java, procedimientos almacenados, documentación Swagger, pruebas y un informe técnico. El alcance elegido es exclusivamente Java.

## 1. Qué necesitas

- **JDK 21 recomendado**, o JDK 17. No basta con Java 8 ni con un JRE. Comprueba `java -version` y `javac -version`.
- SQL Server 2016 SP1 o superior, base con nivel de compatibilidad 130 o superior. Se recomienda SQL Server Developer para el laboratorio.
- SQL Server Management Studio (SSMS) para ejecutar los scripts y administrar el servidor.
- Un servidor SQL en ejecución, TCP/IP habilitado y un puerto conocido, normalmente 1433.
- Maven 3.6.3+ o el Maven Wrapper incluido. La primera compilación necesita Internet.

La configuración no contiene contraseñas. La aplicación escucha en `127.0.0.1:8080` por defecto.

## 2. Preparar la base de datos

### Opción A: SSMS

1. Conéctate a tu instancia SQL Server.
2. Si todavía no tienes las tablas, abre y ejecuta `database/01_base_original.sql` **una sola vez**. Es una copia sin modificaciones del archivo proporcionado.
3. Selecciona **Comisariato** en el selector de base de datos de SSMS.
4. Ejecuta `database/02_objetos_api.sql`. Agrega los dos SP, la vista, la función y la configuración de IVA.
5. Ejecuta `database/03_rol_aplicacion.sql` para crear el rol de permisos de la aplicación.

Si la base ya contiene las tablas del archivo original, comienza en el paso 3. El script original no es una migración repetible: volver a ejecutarlo produce errores de objetos y datos duplicados. El bloque de reinicio al final está comentado; no se necesita para instalar la aplicación.

### Opción B: PowerShell

Desde la raíz de este proyecto:

```powershell
.\scripts\instalar-bd.ps1 -Servidor localhost
```

Utiliza autenticación de Windows para instalar. Para usar un administrador SQL:

```powershell
.\scripts\instalar-bd.ps1 -Servidor 'localhost,1433' -AutenticacionSql
```

El instalador crea la base si falta, instala las tablas solo si está vacía y agrega los objetos del taller. Ante un error se detiene. No elimina datos. Si la instalación del esquema original queda parcialmente ejecutada, revisa ese error en SSMS antes de volver a intentar.

### Usuario de conexión de la aplicación

La ruta más sencilla para JDBC es autenticación de **SQL Server**:

1. En SSMS, habilita el modo de autenticación SQL Server y Windows si tu instancia solo acepta Windows; ese cambio requiere reiniciar el servicio.
2. En Seguridad > Inicios de sesión, crea un login con una contraseña propia.
3. En Asignación de usuarios, asígnalo a Comisariato y al rol `comisariato_app`.
4. En SQL Server Configuration Manager, verifica TCP/IP y el puerto de la instancia. Reinicia el servicio si cambias el protocolo.

No es necesario usar `sa` ni dar `db_owner` a la aplicación. Windows Authentication de SSMS no se traslada automáticamente a JDBC: requeriría la biblioteca nativa oficial y otra configuración. Esta entrega usa usuario y contraseña SQL por variables de entorno.

## 3. Compilar y ejecutar

En PowerShell, desde la carpeta del proyecto:

```powershell
cd java-api
.\mvnw.cmd clean verify
cd ..
.\scripts\iniciar-local.ps1
```

El script pide usuario y contraseña SQL. Para otra instancia o puerto:

```powershell
.\scripts\iniciar-local.ps1 -Servidor localhost -Puerto 1433 -Base Comisariato
```

Si tienes Maven instalado, `mvn clean verify` equivale al comando del wrapper. `JAVA_HOME` debe apuntar a un **JDK 17 o 21**. Si PowerShell bloquea scripts, puedes usar los comandos manuales siguientes sin cambiar la política del sistema.

### Ejecución manual

```powershell
$env:DB_URL='jdbc:sqlserver://localhost:1433;databaseName=Comisariato;encrypt=true;trustServerCertificate=true;loginTimeout=10'
$env:DB_USER='TU_USUARIO_SQL'
# Introduce tu contraseña localmente; no guardes esta línea con la contraseña en Git.
$secret=Read-Host 'Contraseña SQL' -AsSecureString
$cred=[System.Management.Automation.PSCredential]::new($env:DB_USER,$secret)
$env:DB_PASSWORD=$cred.GetNetworkCredential().Password
java -jar .\java-api\target\java-api-1.0.0.jar
```

El paquete también incluye el JAR compilado en `dist/java-api-1.0.0.jar`; puedes ejecutarlo en lugar del archivo de `target`, con las mismas variables. El JAR necesita SQL Server y el JDK, pero no Maven.

`trustServerCertificate=true` se usa en el laboratorio con un certificado autofirmado. La configuración base exige certificado verificado (`false`). En un despliegue real utiliza un certificado confiable. `.env` no se carga automáticamente: usa variables de entorno o un `application-local.yml` privado con el perfil `local`.

La API estará disponible en:

- Swagger UI: <http://localhost:8080/swagger-ui.html>
- OpenAPI JSON: <http://localhost:8080/v3/api-docs>
- Estado de aplicación y BD: <http://localhost:8080/actuator/health>

Para detenerla: `Ctrl+C`. Si el puerto está ocupado, establece `$env:SERVER_PORT='8081'` y utiliza ese puerto también en el menú.

## 4. Abrir el menú Java

Con la API ejecutándose, abre **otra terminal** en la raíz del proyecto:

```powershell
.\scripts\menu.ps1
```

O, sin ejecutar scripts PowerShell:

```powershell
java --source 17 .\java-api\src\main\java\edu\comisariato\api\menu\MenuConsola.java
```

El menú es un cliente HTTP independiente escrito solo con la biblioteca estándar de Java. Tiene estas opciones:

1. Registrar una venta: factura, cliente opcional, empleado, pago y detalles.
2. Anular una venta: solicita su ID y confirma la operación.
3. Consultar productos con stock bajo.
4. Consultar ventas de un empleado.
5. Consultar movimientos de un producto.
0. Salir del menú; la API sigue ejecutándose en su terminal.

Para otro puerto: `.\scripts\menu.ps1 -ApiUrl http://localhost:8081`. Cada resultado muestra el código HTTP y el JSON devuelto, incluidos los mensajes de validación.

## 5. Ejemplo de venta

En Swagger, abre `POST /api/ventas`, pulsa **Try it out** y envía:

```json
{
  "numeroFactura": "FAC-001",
  "clienteId": null,
  "empleadoId": 1,
  "formaPago": "Efectivo",
  "detalles": [
    { "productoId": 1, "cantidad": 2, "descuento": 0.00 }
  ]
}
```

Con los datos originales, el producto 1 cuesta 1.10. Subtotal: **2.20**; IVA académico configurado de 15 %: **0.33**; total: **2.53**. Su stock pasa de 100 a 98. El ID de venta se devuelve en la respuesta; no asumas que es 1 si ya existen datos.

`clienteId: null` representa consumidor final. `descuento` es el importe monetario de descuento de **toda la línea**, no un porcentaje ni un descuento por unidad. `formaPago` admite exactamente `Efectivo`, `Tarjeta` o `Transferencia`. Envía números como números JSON, no como cadenas.

No envíes `total`, `iva`, `subtotal` ni `precioUnitario`: se rechazan con 400. El servidor obtiene los precios y calcula los importes. Cada factura tiene como máximo 100 productos distintos, cada cantidad debe ser entera entre 1 y 1 000 000 y los descuentos no pueden tener más de dos decimales.

Si repites la factura, recibes **409**. Si solicitas más unidades que el stock, recibes **422** y no se crea una venta parcial. Para anular, ejecuta `POST /api/ventas/{ventaId}/anular`: devuelve **200** y restaura existencias; repetir la anulación devuelve **409**.

## 6. Contratos y objetos SQL

| Operación | Endpoint | Objeto SQL | Respuestas principales |
|---|---|---|---|
| Registrar | `POST /api/ventas` | `dbo.usp_RegistrarVenta` | 201 / 400 / 404 / 409 / 422 / 500 |
| Anular | `POST /api/ventas/{ventaId}/anular` | `dbo.usp_AnularVenta` | 200 / 400 / 404 / 409 / 422 / 500 |
| Stock bajo | `GET /api/productos/stock-bajo` | `dbo.vw_ProductosStockBajo` | 200, incluso `[]` |
| Ventas por empleado | `GET /api/empleados/{empleadoId}/ventas` | `dbo.fn_VentasPorEmpleado` | 404 si no existe; 200 `[]` si existe sin ventas |
| Movimientos | `GET /api/productos/{productoId}/movimientos` | `dbo.MovimientosInventario` | 404 si no existe; 200 `[]` si existe sin movimientos |

Se considera stock bajo `Stock <= StockMinimo`, solo para productos activos. Las ventas por empleado incluyen anuladas para conservar el historial. El detalle de contratos y las decisiones no fijadas por el enunciado están en el informe técnico.

## 7. Cómo funciona por dentro

```text
Menú Java / Swagger / cliente HTTP
  -> Controller: recibe JSON y valida anotaciones
  -> Service: reglas, transacción y reintento de deadlock
  -> Repository: SQL explícito y ejecución de procedimientos mediante JdbcTemplate
  -> DatabaseConfig: JdbcTemplate + DataSource configurado por Spring
  -> SQL Server: SP, vista, función y tablas del archivo original
```

`usp_RegistrarVenta` bloquea los productos, valida stock y estado, obtiene precios, calcula subtotal/IVA/total e inserta cabecera, detalles y movimientos de salida. `usp_AnularVenta` bloquea la venta, verifica el estado, devuelve stock y agrega movimientos de entrada. Una anulación conserva factura y detalles para auditoría.

`@Transactional` abre y confirma la transacción del servicio; las excepciones runtime provocan rollback. El interceptor `@Retryable` envuelve la transacción: si SQL Server devuelve 1205, se espera un segundo y se realiza **un único reintento con una transacción nueva**. Un segundo deadlock termina en 409. No se reintentan errores de stock, validación ni factura duplicada.

## 8. Estructura

```text
comisariato-java/
  README.md
  database/
    01_base_original.sql
    02_objetos_api.sql
    03_rol_aplicacion.sql
  java-api/
    pom.xml, mvnw, mvnw.cmd, .mvn/wrapper/
    src/main/java/edu/comisariato/api/
      ComisariatoApplication.java
      config/DatabaseConfig.java
      controller/{Venta,Producto,Empleado}Controller.java
      service/{Venta,Producto}Service.java
      repository/{Venta,Producto}Repository.java
      dto/ (records de entrada, salida y error)
      exception/ (excepciones y GlobalExceptionHandler)
      menu/MenuConsola.java
    src/main/resources/application.yml
    src/main/resources/application.yml.example
    src/test/java/edu/comisariato/api/
  scripts/ (instalación, ejecución y menú)
  docs/ (informe, pruebas y guía del video)
  dist/java-api-1.0.0.jar
```

## 9. Pruebas

### Pruebas automáticas sin servidor SQL

```powershell
cd java-api
.\mvnw.cmd clean verify
```

Verifican los contratos HTTP con servicios simulados, validaciones, traducción de códigos SQL y el orden real de los interceptores de transacción y reintento con un gestor transaccional de prueba. No sustituyen una ejecución de los SP en SQL Server.

### Integración real con SQL Server

Prepara una base separada. Estos tests insertan sus propios datos y los conservan para inspección; no apuntes a una base de uso cotidiano.

```powershell
# Desde la raíz; requiere permiso para crear la base de prueba.
.\scripts\instalar-bd.ps1 -Base ComisariatoTest
$env:DB_URL='jdbc:sqlserver://localhost:1433;databaseName=ComisariatoTest;encrypt=true;trustServerCertificate=true'
$env:DB_USER='TU_USUARIO_DE_PRUEBAS'
# Configura DB_PASSWORD igual que en la sección de ejecución manual.
$env:RUN_SQL_INTEGRATION='true'
cd java-api
.\mvnw.cmd verify
Remove-Item Env:RUN_SQL_INTEGRATION
```

El usuario de pruebas necesita permisos de inserción/consulta sobre las tablas para preparar y verificar fixtures, además de ejecutar SP; usa un usuario de laboratorio con `db_owner` **solo en ComisariatoTest**. La aplicación normal utiliza `comisariato_app`. Los tests reales se niegan a ejecutarse si `DB_NAME()` no empieza con `ComisariatoTest`.

Comprueban totales, stock, duplicados, rollback completo, anulaciones, movimientos y dos ventas simultáneas disputando una unidad. Sin `RUN_SQL_INTEGRATION=true`, se omiten de forma explícita. Consulta `docs/VALIDACION.md` para conocer qué se ejecutó al preparar esta entrega.

## 10. Problemas frecuentes

| Síntoma | Solución |
|---|---|
| `java` muestra 1.8 | Instala/selecciona JDK 17 o 21 y corrige `PATH` y `JAVA_HOME`. |
| Conexión rechazada o timeout | Inicia el servicio SQL Server, confirma instancia, TCP/IP y puerto. |
| `Login failed` | Revisa modo mixto, login SQL, contraseña y usuario asignado a la base. |
| Error de certificado | Para laboratorio usa `iniciar-local.ps1`; en otro entorno instala certificado confiable. |
| SP/vista/función inexistentes | Ejecuta `02_objetos_api.sql` en la misma base indicada por `DB_URL`. |
| Swagger abre pero health es DOWN | El servidor HTTP funciona; falta resolver la conexión a BD. |
| Productos de stock bajo devuelve `[]` | Los siete productos iniciales están por encima del mínimo. Es una respuesta correcta. |
| Factura duplicada | Usa un nuevo número; anular no libera el número de factura. |
| Maven no descarga dependencias | Revisa conexión, proxy y certificados de tu entorno. |

## 11. Entrega académica

- Código preparado como repositorio Git local. La publicación remota queda a cargo del titular de la cuenta.
- Informe técnico: `docs/INFORME_TECNICO.md` y su versión PDF.
- Guion y pasos para grabar un video real: `docs/GUIA_VIDEO.md`.
- Evidencias y límites de verificación: `docs/VALIDACION.md`.

No se incluye una aplicación .NET porque se eligió expresamente la alternativa Java. No se presenta un guion como si fuera un video ya grabado.

## Referencias técnicas

- Requisitos de Spring Boot 3.5: <https://docs.spring.io/spring-boot/3.5/system-requirements.html>
- Springdoc para Spring Boot 3: <https://springdoc.org/v2/>
- Controlador oficial JDBC: <https://learn.microsoft.com/en-us/sql/connect/jdbc/release-notes-for-the-jdbc-driver>

Las versiones de las dependencias están fijadas en `pom.xml` para reproducir la entrega; no se afirma que sean las más recientes.
