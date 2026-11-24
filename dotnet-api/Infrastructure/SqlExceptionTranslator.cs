using Comisariato.Api.Infrastructure.Exceptions;
using Microsoft.Data.SqlClient;

namespace Comisariato.Api.Infrastructure;

/// <summary>
/// Traduce los errores lanzados por los procedimientos almacenados y las
/// restricciones de SQL Server a excepciones de dominio. Los deadlocks (1205)
/// se dejan pasar como SqlException para que la política de Polly los reintente.
/// </summary>
public static class SqlExceptionTranslator
{
    public const int DeadlockErrorNumber = 1205;

    public static bool TryMapRegistrarVenta(SqlException exception, out Exception mapped)
    {
        switch (exception.Number)
        {
            case 50001:
                mapped = new BusinessRuleException(exception.Message);
                return true;

            case 50002:
                mapped = new BusinessRuleException("Stock insuficiente para la venta.");
                return true;

            case 2627:
            case 2601:
                mapped = new ConflictException("Ya existe una venta con el NumeroFactura indicado.");
                return true;

            case 547:
                mapped = new InputValidationException(
                    "El ClienteID, EmpleadoID o ProductoID indicado no existe.");
                return true;

            case 8152:
            case 2628:
                mapped = new InputValidationException(
                    "Uno de los valores enviados supera la longitud permitida por la base de datos.");
                return true;

            default:
                mapped = null!;
                return false;
        }
    }

    public static bool TryMapAnularVenta(SqlException exception, out Exception mapped)
    {
        switch (exception.Number)
        {
            case 50001:
                mapped = new NotFoundException("La venta no existe.");
                return true;

            case 50002:
                mapped = new ConflictException("La venta no está completada o ya fue anulada.");
                return true;

            case 547:
                mapped = new InputValidationException("El EmpleadoID indicado no existe.");
                return true;

            default:
                mapped = null!;
                return false;
        }
    }
}
