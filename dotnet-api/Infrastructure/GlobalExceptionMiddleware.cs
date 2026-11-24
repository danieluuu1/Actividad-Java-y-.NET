using Comisariato.Api.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Comisariato.Api.Infrastructure;

/// <summary>
/// Middleware global que captura todas las excepciones no controladas y
/// responde con ProblemDetails (RFC 7807) usando el código HTTP coherente:
/// 400 validación, 404 no encontrado, 409 conflicto, 422 regla de negocio y
/// 500 error inesperado.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "La solicitud {Method} {Path} fue cancelada por el cliente.",
                context.Request.Method,
                context.Request.Path);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogError(
                exception,
                "Error después de iniciar la respuesta en {Method} {Path}. No se puede escribir ProblemDetails.",
                context.Request.Method,
                context.Request.Path);
            return;
        }

        var (statusCode, title, detail) = MapException(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Error {StatusCode} procesando {Method} {Path}. TraceId: {TraceId}",
                statusCode,
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Solicitud rechazada con {StatusCode} en {Method} {Path}: {Message}. TraceId: {TraceId}",
                statusCode,
                context.Request.Method,
                context.Request.Path,
                exception.Message,
                context.TraceIdentifier);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Type = $"https://httpstatuses.io/{statusCode}"
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(
            problem,
            cancellationToken: context.RequestAborted);
    }

    private static (int StatusCode, string Title, string Detail) MapException(Exception exception)
        => exception switch
        {
            NotFoundException notFound
                => (StatusCodes.Status404NotFound, "Recurso no encontrado.", notFound.Message),

            ConflictException conflict
                => (StatusCodes.Status409Conflict, "Conflicto de estado.", conflict.Message),

            BusinessRuleException businessRule
                => (StatusCodes.Status422UnprocessableEntity, "Regla de negocio violada.", businessRule.Message),

            InputValidationException validation
                => (StatusCodes.Status400BadRequest, "Solicitud inválida.", validation.Message),

            SqlException { Number: SqlExceptionTranslator.DeadlockErrorNumber }
                => (StatusCodes.Status409Conflict,
                    "Conflicto de concurrencia.",
                    "La operación no pudo completarse por un bloqueo de la base de datos. Intente nuevamente."),

            SqlException
                => (StatusCodes.Status500InternalServerError,
                    "Error de base de datos.",
                    "Ocurrió un error inesperado al comunicarse con la base de datos."),

            BadHttpRequestException badRequest
                => (StatusCodes.Status400BadRequest, "Solicitud inválida.", badRequest.Message),

            _ => (StatusCodes.Status500InternalServerError,
                "Error interno del servidor.",
                "Ocurrió un error inesperado al procesar la solicitud.")
        };
}
