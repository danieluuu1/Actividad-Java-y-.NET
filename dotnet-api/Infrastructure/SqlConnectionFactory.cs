using Microsoft.Data.SqlClient;

namespace Comisariato.Api.Infrastructure;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}

/// <summary>
/// Crea conexiones ADO.NET hacia la base de datos Comisariato usando la
/// cadena de conexión configurada en appsettings o variables de entorno.
/// </summary>
public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se configuró la cadena de conexión 'ConnectionStrings:DefaultConnection'.");
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}
