namespace Comisariato.Api.Infrastructure.Exceptions;

/// <summary>Recurso inexistente en la base de datos (HTTP 404).</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
