using System.Data;
using Comisariato.Api.DTOs;
using Comisariato.Api.Infrastructure;
using Microsoft.Data.SqlClient;

namespace Comisariato.Api.Repositories;

public sealed class VentaRepository : IVentaRepository
{
    private const int CommandTimeoutSeconds = 30;

    private readonly ISqlConnectionFactory _connectionFactory;

    public VentaRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<int> RegistrarVentaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        VentaRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new SqlCommand("dbo.usp_RegistrarVenta", connection, transaction)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = CommandTimeoutSeconds
            };

            command.Parameters.Add("@NumeroFactura", SqlDbType.VarChar, 20).Value = request.NumeroFactura;
            command.Parameters.Add("@ClienteID", SqlDbType.Int).Value =
                request.ClienteID.HasValue ? request.ClienteID.Value : DBNull.Value;
            command.Parameters.Add("@EmpleadoID", SqlDbType.Int).Value = request.EmpleadoID;
            command.Parameters.Add("@FormaPago", SqlDbType.NVarChar, 30).Value = request.FormaPago;
            command.Parameters.Add("@ProductoID", SqlDbType.Int).Value = request.ProductoID;
            command.Parameters.Add("@Cantidad", SqlDbType.Int).Value = request.Cantidad;

            var ventaIdParameter = command.Parameters.Add("@VentaID", SqlDbType.Int);
            ventaIdParameter.Direction = ParameterDirection.Output;

            await command.ExecuteNonQueryAsync(cancellationToken);

            var ventaId = ventaIdParameter.Value is int value ? value : 0;
            if (ventaId <= 0)
            {
                throw new InvalidOperationException(
                    "El procedimiento usp_RegistrarVenta no devolvió un VentaID válido.");
            }

            return ventaId;
        }
        catch (SqlException exception) when (SqlExceptionTranslator.TryMapRegistrarVenta(exception, out var mapped))
        {
            throw mapped;
        }
    }

    public async Task<VentaResponse?> ObtenerVentaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int ventaId,
        CancellationToken cancellationToken)
    {
        VentaResponse? venta = null;

        await using (var command = new SqlCommand(
            """
            SELECT VentaID, NumeroFactura, ClienteID, EmpleadoID, FechaVenta,
                   Subtotal, IVA, Total, FormaPago, Estado
            FROM dbo.Ventas
            WHERE VentaID = @VentaID;
            """,
            connection,
            transaction))
        {
            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.Add("@VentaID", SqlDbType.Int).Value = ventaId;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var oVentaID = reader.GetOrdinal("VentaID");
                var oNumeroFactura = reader.GetOrdinal("NumeroFactura");
                var oClienteID = reader.GetOrdinal("ClienteID");
                var oEmpleadoID = reader.GetOrdinal("EmpleadoID");
                var oFechaVenta = reader.GetOrdinal("FechaVenta");
                var oSubtotal = reader.GetOrdinal("Subtotal");
                var oIVA = reader.GetOrdinal("IVA");
                var oTotal = reader.GetOrdinal("Total");
                var oFormaPago = reader.GetOrdinal("FormaPago");
                var oEstado = reader.GetOrdinal("Estado");

                venta = new VentaResponse(
                    VentaID: reader.GetInt32(oVentaID),
                    NumeroFactura: reader.GetString(oNumeroFactura),
                    ClienteID: reader.IsDBNull(oClienteID) ? null : reader.GetInt32(oClienteID),
                    EmpleadoID: reader.GetInt32(oEmpleadoID),
                    FechaVenta: reader.GetDateTime(oFechaVenta),
                    Subtotal: reader.GetDecimal(oSubtotal),
                    IVA: reader.GetDecimal(oIVA),
                    Total: reader.GetDecimal(oTotal),
                    FormaPago: reader.GetString(oFormaPago),
                    Estado: reader.GetString(oEstado),
                    Detalle: Array.Empty<VentaDetalleResponse>());
            }
        }

        if (venta is null)
        {
            return null;
        }

        var detalle = new List<VentaDetalleResponse>();

        await using (var command = new SqlCommand(
            """
            SELECT ProductoID, Cantidad, PrecioUnitario, Descuento, SubtotalLinea
            FROM dbo.DetalleVenta
            WHERE VentaID = @VentaID
            ORDER BY DetalleVentaID;
            """,
            connection,
            transaction))
        {
            command.CommandTimeout = CommandTimeoutSeconds;
            command.Parameters.Add("@VentaID", SqlDbType.Int).Value = ventaId;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var oProductoID = reader.GetOrdinal("ProductoID");
            var oCantidad = reader.GetOrdinal("Cantidad");
            var oPrecioUnitario = reader.GetOrdinal("PrecioUnitario");
            var oDescuento = reader.GetOrdinal("Descuento");
            var oSubtotalLinea = reader.GetOrdinal("SubtotalLinea");

            while (await reader.ReadAsync(cancellationToken))
            {
                detalle.Add(new VentaDetalleResponse(
                    ProductoID: reader.GetInt32(oProductoID),
                    Cantidad: reader.GetInt32(oCantidad),
                    PrecioUnitario: reader.GetDecimal(oPrecioUnitario),
                    Descuento: reader.GetDecimal(oDescuento),
                    SubtotalLinea: reader.GetDecimal(oSubtotalLinea)));
            }
        }

        return venta with { Detalle = detalle };
    }

    public async Task AnularVentaAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        int ventaId,
        int? empleadoId,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var command = new SqlCommand("dbo.usp_AnularVenta", connection, transaction)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = CommandTimeoutSeconds
            };

            command.Parameters.Add("@VentaID", SqlDbType.Int).Value = ventaId;
            command.Parameters.Add("@EmpleadoID", SqlDbType.Int).Value =
                empleadoId.HasValue ? empleadoId.Value : DBNull.Value;

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException exception) when (SqlExceptionTranslator.TryMapAnularVenta(exception, out var mapped))
        {
            throw mapped;
        }
    }

    public async Task<bool> EmpleadoExisteAsync(int empleadoId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Empleados WHERE EmpleadoID = @EmpleadoID) THEN 1 ELSE 0 END;",
            connection);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.Add("@EmpleadoID", SqlDbType.Int).Value = empleadoId;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && Convert.ToInt32(result) == 1;
    }

    public async Task<IReadOnlyList<VentaEmpleadoResponse>> ObtenerVentasPorEmpleadoAsync(
        int empleadoId,
        CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT VentaID, NumeroFactura, FechaVenta, Total
            FROM dbo.fn_VentasPorEmpleado(@EmpleadoID)
            ORDER BY FechaVenta DESC, VentaID DESC;
            """,
            connection);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.Add("@EmpleadoID", SqlDbType.Int).Value = empleadoId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var oVentaID = reader.GetOrdinal("VentaID");
        var oNumeroFactura = reader.GetOrdinal("NumeroFactura");
        var oFechaVenta = reader.GetOrdinal("FechaVenta");
        var oTotal = reader.GetOrdinal("Total");

        var ventas = new List<VentaEmpleadoResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            ventas.Add(new VentaEmpleadoResponse(
                VentaID: reader.GetInt32(oVentaID),
                NumeroFactura: reader.GetString(oNumeroFactura),
                FechaVenta: reader.GetDateTime(oFechaVenta),
                Total: reader.GetDecimal(oTotal)));
        }

        return ventas;
    }
}
