namespace Comisariato.Api.Infrastructure.Exceptions;

/// <summary>Datos de entrada inválidos detectados fuera del model binding (HTTP 400).</summary>
public sealed class InputValidationException : Exception
{
    public InputValidationException(string message) : base(message)
    {
    }
}
