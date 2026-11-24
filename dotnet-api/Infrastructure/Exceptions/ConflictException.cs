namespace Comisariato.Api.Infrastructure.Exceptions;

/// <summary>Conflicto de estado, por ejemplo factura duplicada o venta ya anulada (HTTP 409).</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
