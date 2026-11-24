using Comisariato.Api.DTOs;
using Comisariato.Api.Infrastructure;
using Comisariato.Api.Infrastructure.Exceptions;
using Comisariato.Api.Repositories;
using Microsoft.Data.SqlClient;

namespace Comisariato.Api.Services;

public sealed class VentaService : IVentaService
{
    private readonly IVentaRepository _repository;
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly PollyPolicies _pollyPolicies;
    private readonly ILogger<VentaService> _logger;

    public VentaService(
        IVentaRepository repository,
        ISqlConnectionFactory connectionFactory,
        PollyPolicies pollyPolicies,
        ILogger<VentaService> logger)
    {
        _repository = repository;
        _connectionFactory = connectionFactory;
        _pollyPolicies = pollyPolicies;
        _logger = logger;
    }

    public async Task<VentaResponse> RegistrarVentaAsync(
        VentaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var venta = await _pollyPolicies.DeadlockRetry.ExecuteAsync(
                async token =>
                {
                    await using var connection = _connectionFactory.CreateConnection();
                    await connection.OpenAsync(token);

                    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);
                    try
                    {
                        var ventaId = await _repository.RegistrarVentaAsync(
                            connection,
                            transaction,
                            request,
                            token);

                        var creada = await _repository.ObtenerVentaAsync(
                            connection,
                            transaction,
                            ventaId,
                            token)
                            ?? throw new InvalidOperationException(
                                $"No se pudo recuperar la venta {ventaId} recién registrada.");

                        await transaction.CommitAsync(token);
                        return creada;
                    }
                    catch
                    {
                        await RollbackQuietlyAsync(transaction);
                        throw;
                    }
                },
                cancellationToken);

            _logger.LogInformation(
                "Venta registrada correctamente. VentaID={VentaID}, NumeroFactura={NumeroFactura}, Total={Total}.",
                venta.VentaID,
                venta.NumeroFactura,
                venta.Total);

            return venta;
        }
        catch (SqlException exception) when (exception.Number == SqlExceptionTranslator.DeadlockErrorNumber)
        {
            _logger.LogError(
                exception,
                "Deadlock persistente al registrar la venta {NumeroFactura} después del reintento.",
                request.NumeroFactura);

            throw new ConflictException(
                "La operación no pudo completarse por un conflicto de concurrencia. Intente nuevamente.");
        }
    }

    public async Task<AnularVentaResponse> AnularVentaAsync(
        int ventaId,
        int? empleadoId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _pollyPolicies.DeadlockRetry.ExecuteAsync(
                async token =>
                {
                    await using var connection = _connectionFactory.CreateConnection();
                    await connection.OpenAsync(token);

                    await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(token);
                    try
                    {
                        await _repository.AnularVentaAsync(
                            connection,
                            transaction,
                            ventaId,
                            empleadoId,
                            token);

                        await transaction.CommitAsync(token);
                    }
                    catch
                    {
                        await RollbackQuietlyAsync(transaction);
                        throw;
                    }
                },
                cancellationToken);

            _logger.LogInformation("Venta anulada correctamente. VentaID={VentaID}.", ventaId);

            return new AnularVentaResponse(
                VentaID: ventaId,
                Estado: "Anulada",
                Mensaje: "Venta anulada correctamente.");
        }
        catch (SqlException exception) when (exception.Number == SqlExceptionTranslator.DeadlockErrorNumber)
        {
            _logger.LogError(
                exception,
                "Deadlock persistente al anular la venta {VentaID} después del reintento.",
                ventaId);

            throw new ConflictException(
                "La operación no pudo completarse por un conflicto de concurrencia. Intente nuevamente.");
        }
    }

    public async Task<IReadOnlyList<VentaEmpleadoResponse>> ObtenerVentasPorEmpleadoAsync(
        int empleadoId,
        CancellationToken cancellationToken)
    {
        var existe = await _pollyPolicies.DeadlockRetry.ExecuteAsync(
            token => _repository.EmpleadoExisteAsync(empleadoId, token),
            cancellationToken);

        if (!existe)
        {
            throw new NotFoundException($"El empleado con ID {empleadoId} no existe.");
        }

        var ventas = await _pollyPolicies.DeadlockRetry.ExecuteAsync(
            token => _repository.ObtenerVentasPorEmpleadoAsync(empleadoId, token),
            cancellationToken);

        _logger.LogInformation(
            "Consulta de ventas del empleado {EmpleadoID}: {Cantidad} registros.",
            empleadoId,
            ventas.Count);

        return ventas;
    }

    /// <summary>
    /// Revierte la transacción sin enmascarar la excepción original. Los SP
    /// también abren y revierten su propia transacción, por lo que el rollback
    /// puede haber ocurrido ya; ese caso se ignora de forma segura.
    /// </summary>
    private async Task RollbackQuietlyAsync(SqlTransaction transaction)
    {
        try
        {
            if (transaction.Connection is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
        }
        catch (InvalidOperationException)
        {
            // La transacción ya fue completada/revertida por el procedimiento almacenado.
        }
        catch (SqlException exception)
        {
            _logger.LogWarning(
                exception,
                "No se pudo revertir explícitamente la transacción; el procedimiento ya la había revertido.");
        }
    }
}
