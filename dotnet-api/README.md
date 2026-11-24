# Comisariato API (.NET 8)

API REST en ASP.NET Core 8 con ADO.NET (`Microsoft.Data.SqlClient`) sobre la base
de datos `Comisariato`. Sigue la arquitectura Controller → Service → Repository →
Infrastructure, sin Entity Framework Core.

## Requisitos

- .NET SDK 8.0.428 (fijado en `../global.json`).
- SQL Server local con la base `Comisariato` creada con `../script.sql`.
- Autenticación Windows (`Trusted_Connection=True`).

## Configuración

La cadena de conexión se toma de `ConnectionStrings:DefaultConnection`:

```json
"Server=localhost;Database=Comisariato;Trusted_Connection=True;TrustServerCertificate=True;"
```

- `appsettings.json` / `appsettings.Development.json`: configuración local.
- `appsettings.example.json`: plantilla para el repositorio, sin credenciales.
- Se puede sobrescribir con variables de entorno, por ejemplo:
  `ConnectionStrings__DefaultConnection=...`

## Ejecución

```powershell
dotnet restore
dotnet run
```

- Swagger UI: http://localhost:5139/swagger
- Health check: http://localhost:5139/health

## Endpoints

| Método | Ruta | Respuestas |
| --- | --- | --- |
| POST | `/api/ventas` | 201, 400, 409 (factura duplicada), 422 (stock insuficiente), 500 |
| POST | `/api/ventas/{ventaId}/anular` | 200, 404, 409 (ya anulada), 500 |
| GET | `/api/productos/stock-bajo` | 200 |
| GET | `/api/empleados/{empleadoId}/ventas` | 200 (array vacío si no tiene ventas), 404 |
| GET | `/api/productos/{productoId}/movimientos` | 200, 404 |

### Ejemplo de registro de venta

```json
{
  "numeroFactura": "F-0002",
  "clienteID": 1,
  "empleadoID": 1,
  "formaPago": "Efectivo",
  "productoID": 1,
  "cantidad": 2
}
```

El `Subtotal`, `IVA` y `Total` **no** se envían: los calcula
`dbo.usp_RegistrarVenta` en el servidor.

La anulación acepta un cuerpo opcional para la bitácora:

```json
{ "empleadoID": 2 }
```

También se puede enviar `?empleadoId=2` como query string; si se omite, el
movimiento de devolución de stock se registra sin empleado.

## Manejo de errores

Todas las respuestas de error usan `ProblemDetails` (RFC 7807,
`application/problem+json`). El middleware global traduce:

| Excepción / error SQL | HTTP |
| --- | --- |
| Validación de Data Annotations | 400 |
| `InputValidationException` | 400 |
| `NotFoundException` / `THROW 50001` al anular | 404 |
| `ConflictException` / factura duplicada (2627/2601) / venta ya anulada | 409 |
| `BusinessRuleException` / stock insuficiente (`THROW 50002`) | 422 |
| Deadlock 1205 tras el reintento | 409 |
| Error inesperado | 500 |

Los deadlocks (error 1205) se reintentan una vez después de 1 segundo mediante
`PollyPolicies.DeadlockRetry` (`WaitAndRetryAsync`).

## Estructura

```
dotnet-api/
├── Comisariato.Api.csproj
├── Program.cs
├── appsettings.json
├── appsettings.Development.json
├── appsettings.example.json
├── Controllers/
├── Services/
├── Repositories/
├── DTOs/
├── Infrastructure/
└── Models/
```
