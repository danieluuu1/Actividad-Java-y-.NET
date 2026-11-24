using Microsoft.Data.SqlClient;
using Polly;
using Polly.Retry;

namespace Comisariato.Api.Infrastructure;

/// <summary>
/// Política Polly de reintento ante deadlocks de SQL Server (error 1205):
/// un reintento después de 1 segundo. Si vuelve a fallar, la excepción
/// original se propaga y el middleware la convierte en 409.
/// </summary>
public sealed class PollyPolicies
{
    public PollyPolicies(ILogger<PollyPolicies> logger)
    {
        DeadlockRetry = Policy
            .Handle<SqlException>(exception => exception.Number == SqlExceptionTranslator.DeadlockErrorNumber)
            .WaitAndRetryAsync(
                retryCount: 1,
                sleepDurationProvider: _ => TimeSpan.FromSeconds(1),
                onRetry: (exception, delay, attempt, _) =>
                    logger.LogWarning(
                        exception,
                        "Deadlock detectado (SQL error 1205). Reintento {Attempt} de 1 en {DelayMs} ms.",
                        attempt,
                        delay.TotalMilliseconds));
    }

    public AsyncRetryPolicy DeadlockRetry { get; }
}
