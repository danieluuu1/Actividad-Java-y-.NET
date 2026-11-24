namespace Comisariato.Api.Infrastructure.Exceptions;

/// <summary>Regla de negocio violada, por ejemplo stock insuficiente (HTTP 422).</summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
