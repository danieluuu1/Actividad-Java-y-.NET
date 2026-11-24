using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Comisariato.Api.Infrastructure;

/// <summary>
/// Health check que verifica que la base de datos Comisariato responda.
/// </summary>
public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlServerHealthCheck(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = _connectionFactory.CreateConnection();
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("SELECT 1;", connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result is not null && Convert.ToInt32(result) == 1
                ? HealthCheckResult.Healthy("Base de datos Comisariato accesible.")
                : HealthCheckResult.Unhealthy("La base de datos respondió de forma inesperada.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "No se pudo conectar con la base de datos Comisariato.",
                exception);
        }
    }
}
