using System.Net;
using System.Text.Json;
using Service.Operaciones.Application.Common;
using Service.Operaciones.Application.Common.Exceptions;

namespace Service.Operaciones.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        (int statusCode, string title, List<string> errors) = exception switch
        {
            ValidationException validationEx => (
                (int)HttpStatusCode.BadRequest,
                "Error de validación",
                validationEx.Errors != null && validationEx.Errors.Count > 0
                    ? validationEx.Errors
                    : [validationEx.Message]
            ),
            ArgumentException argumentEx => (
                (int)HttpStatusCode.BadRequest,
                "Error de argumento",
                [argumentEx.Message]
            ),
            FluentValidation.ValidationException fluentEx => (
                (int)HttpStatusCode.BadRequest,
                "Error de validación",
                fluentEx.Errors.Select(e => e.ErrorMessage).ToList()
            ),
            NotFoundException notFoundEx => (
                (int)HttpStatusCode.NotFound,
                "No encontrado",
                [notFoundEx.Message]
            ),
            UnauthorizedException unauthorizedEx => (
                (int)HttpStatusCode.Forbidden,
                "No autorizado",
                [unauthorizedEx.Message]
            ),
            UnauthorizedAccessException => (
                (int)HttpStatusCode.Unauthorized,
                "No autorizado",
                ["Acceso no autorizado."]
            ),
            _ => (
                (int)HttpStatusCode.InternalServerError,
                "Error interno",
                ["Error en el proceso"]
            )
        };

        if (statusCode == (int)HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "{Title}: {Message}", title, exception.Message);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = new ApiResponse<object>
        {
            Data = null,
            Success = false,
            Errors = errors
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
