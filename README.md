# Actividad: Comisariato (Java y .NET)

Repositorio que contiene las implementaciones de la actividad de integración para la base de datos **Comisariato** en Microsoft SQL Server.

## Estructura del repositorio

```text
Actividad-Java-y-.NET/
├── dotnet-api/          # API REST en ASP.NET Core 8 con ADO.NET
└── comisariato-java/    # API REST en Java 17+ / Spring Boot 3 con Spring JDBC
    ├── java-api/        # Código fuente Maven y aplicación Spring Boot
    ├── database/        # Scripts SQL (01 base original, 02 objetos API, 03 rol)
    ├── docs/            # Informe técnico, contratos OpenAPI y validaciones
    └── scripts/         # Scripts PowerShell y de ejecución rápida
```

---

## Proyectos

### 1. [comisariato-java/](./comisariato-java/)
- **Stack:** Java 17+, Spring Boot 3.5.8, Maven, Spring JDBC (`JdbcTemplate`), SQL Server.
- **Documentación Swagger:** `http://localhost:8080/swagger-ui.html`
- **Menú interactivo de consola:** `.\scripts\menu.ps1` o ejecutando `menu.bat`.
- **Ejecución rápida:** `iniciar.bat` o `.\scripts\iniciar-rapido.ps1`.
- Consulta el [README de Java](./comisariato-java/README.md) para más detalles.

### 2. [dotnet-api/](./dotnet-api/)
- **Stack:** .NET 8 (ASP.NET Core Web API), ADO.NET (`Microsoft.Data.SqlClient`), SQL Server.
- **Documentación Swagger:** `http://localhost:5139/swagger`
- **Ejecución:** `dotnet run` dentro de `dotnet-api/`.
- Consulta el [README de .NET](./dotnet-api/README.md) para más detalles.
