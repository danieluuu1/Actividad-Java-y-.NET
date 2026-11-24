using System.Data;
using Comisariato.Api.DTOs;
using Comisariato.Api.Infrastructure;
using Microsoft.Data.SqlClient;

namespace Comisariato.Api.Repositories;

public sealed class ProductoRepository : IProductoRepository
{
    private const int CommandTimeoutSeconds = 30;

    private readonly ISqlConnectionFactory _connectionFactory;

    public ProductoRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ProductoStockBajoResponse>> ObtenerStockBajoAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT ProductoID, Nombre, Stock, StockMinimo, Categoria
            FROM dbo.vw_ProductosStockBajo
            ORDER BY Stock ASC, Nombre ASC;
            """,
            connection);
        command.CommandTimeout = CommandTimeoutSeconds;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var oProductoID = reader.GetOrdinal("ProductoID");
        var oNombre = reader.GetOrdinal("Nombre");
        var oStock = reader.GetOrdinal("Stock");
        var oStockMinimo = reader.GetOrdinal("StockMinimo");
        var oCategoria = reader.GetOrdinal("Categoria");

        var productos = new List<ProductoStockBajoResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            productos.Add(new ProductoStockBajoResponse(
                ProductoID: reader.GetInt32(oProductoID),
                Nombre: reader.GetString(oNombre),
                Stock: reader.GetInt32(oStock),
                StockMinimo: reader.GetInt32(oStockMinimo),
                Categoria: reader.GetString(oCategoria)));
        }

        return productos;
    }

    public async Task<bool> ExisteAsync(int productoId, CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Productos WHERE ProductoID = @ProductoID) THEN 1 ELSE 0 END;",
            connection);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.Add("@ProductoID", SqlDbType.Int).Value = productoId;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null && Convert.ToInt32(result) == 1;
    }

    public async Task<IReadOnlyList<MovimientoInventarioResponse>> ObtenerMovimientosAsync(
        int productoId,
        CancellationToken cancellationToken)
    {
        await using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(
            """
            SELECT MovimientoID, ProductoID, TipoMovimiento, Cantidad,
                   StockAnterior, StockNuevo, ReferenciaTipo, ReferenciaID,
                   FechaMovimiento, EmpleadoID
            FROM dbo.MovimientosInventario
            WHERE ProductoID = @ProductoID
            ORDER BY FechaMovimiento DESC, MovimientoID DESC;
            """,
            connection);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.Add("@ProductoID", SqlDbType.Int).Value = productoId;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var oMovimientoID = reader.GetOrdinal("MovimientoID");
        var oProductoID = reader.GetOrdinal("ProductoID");
        var oTipoMovimiento = reader.GetOrdinal("TipoMovimiento");
        var oCantidad = reader.GetOrdinal("Cantidad");
        var oStockAnterior = reader.GetOrdinal("StockAnterior");
        var oStockNuevo = reader.GetOrdinal("StockNuevo");
        var oReferenciaTipo = reader.GetOrdinal("ReferenciaTipo");
        var oReferenciaID = reader.GetOrdinal("ReferenciaID");
        var oFechaMovimiento = reader.GetOrdinal("FechaMovimiento");
        var oEmpleadoID = reader.GetOrdinal("EmpleadoID");

        var movimientos = new List<MovimientoInventarioResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            movimientos.Add(new MovimientoInventarioResponse(
                MovimientoID: reader.GetInt64(oMovimientoID),
                ProductoID: reader.GetInt32(oProductoID),
                TipoMovimiento: reader.GetString(oTipoMovimiento),
                Cantidad: reader.GetInt32(oCantidad),
                StockAnterior: reader.GetInt32(oStockAnterior),
                StockNuevo: reader.GetInt32(oStockNuevo),
                ReferenciaTipo: reader.IsDBNull(oReferenciaTipo) ? null : reader.GetString(oReferenciaTipo),
                ReferenciaID: reader.IsDBNull(oReferenciaID) ? null : reader.GetInt32(oReferenciaID),
                FechaMovimiento: reader.GetDateTime(oFechaMovimiento),
                EmpleadoID: reader.IsDBNull(oEmpleadoID) ? null : reader.GetInt32(oEmpleadoID)));
        }

        return movimientos;
    }
}
