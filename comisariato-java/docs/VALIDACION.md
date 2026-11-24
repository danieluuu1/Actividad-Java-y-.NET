# Verificación de la entrega

Fecha: **24 de noviembre de 2026**, zona America/Guayaquil.

## Compilación y pruebas Java

Resultado: **BUILD SUCCESS** usando Microsoft OpenJDK 21.0.13.1, Maven 3.9.11 y bytecode objetivo Java 17.

| Clase | Pruebas aprobadas | Fallos | Errores | Omitidas |
|---|---:|---:|---:|---:|
| ApiContractTest | 34 | 0 | 0 | 0 |
| SqlErrorTranslatorTest | 2 | 0 | 0 | 0 |
| TransactionRetryTest | 6 | 0 | 0 | 0 |
| SqlServerIntegrationTest | 0 | 0 | 0 | 2 |
| Total | **42** | **0** | **0** | **2** |

Las pruebas de contratos usan MockMvc y servicios simulados. Las pruebas de transacción utilizan proxies reales de Spring y un gestor transaccional de prueba; verifican rollback/commit e intentos desde la perspectiva del framework. No prueban la persistencia física en SQL Server. Mockito se carga como agente al iniciar los tests para evitar problemas de auto-attach en entornos restringidos.

Se generó el JAR ejecutable Spring Boot y se incluyó en `dist/java-api-1.0.0.jar`. El resumen de Maven está en `evidencia_maven.txt`.

## Arranque real del JAR y HTTP

Se ejecutó el JAR en un proceso real, en un puerto local de pruebas, y se verificó:

- `/v3/api-docs`: 200, con las cinco rutas requeridas.
- `/swagger-ui.html`: 200 tras la redirección habitual a Swagger UI.
- `POST /api/ventas` con `{}`: 400 por validación.
- `POST /api/ventas/0/anular`: 400 por ID no positivo.
- `/actuator/health`: 503 y estado DOWN, coherente con la ausencia de conexión SQL.
- Menú Java: compilación en modo archivo fuente, presentación de las cinco opciones y salida normal con código 0.

Las respuestas se registraron en `evidencia_http.json`. `openapi.json` se obtuvo del servidor real iniciado para la verificación; en la copia distribuida se normalizó únicamente la dirección del servidor a localhost:8080. El proceso de prueba se detuvo después de los chequeos.

## SQL y scripts

Los tres archivos SQL se analizaron con **Microsoft.SqlServer.TransactSql.ScriptDom.TSql170Parser**, con **cero errores de sintaxis**. Esta revisión no ejecuta SQL ni comprueba objetos, permisos, bloqueos, commits o rollback en el motor.

Los cuatro scripts PowerShell pasaron el parser de PowerShell con cero errores de sintaxis. El archivo `01_base_original.sql` conserva los bytes del SQL proporcionado.

## Informe

Se generó el informe técnico en PDF y se revisó visualmente su renderizado. Incluye arquitectura, contratos, matriz de requisitos, procedimientos, decisiones y revisión del esquema original. La versión Markdown permite editarlo.

## Pendiente de comprobar con una base activa

No se pudo ejecutar una integración real contra SQL Server en esta sesión: la instancia local no aceptó conexión en 1433 y LocalDB no estuvo accesible desde el entorno de trabajo. No se utilizaron ni inventaron credenciales y no se modificó una base existente del usuario.

Por tanto, **la ejecución de los SP, la persistencia, el rollback real del motor y la concurrencia real permanecen sin verificar**. Las dos pruebas de `SqlServerIntegrationTest` están listas y documentadas en README.md para ejecutarse con una BD `ComisariatoTest` y `RUN_SQL_INTEGRATION=true`. Un health 503 no es evidencia de conexión satisfactoria.

## Entrega académica restante

El repositorio Git es local; no se publicó en una cuenta remota. Se entrega un guion detallado para el video, no una grabación. Una vez configurada la base real, corresponde ejecutar la integración, grabar la demostración y publicar el repositorio según el canal que solicite la docente.
