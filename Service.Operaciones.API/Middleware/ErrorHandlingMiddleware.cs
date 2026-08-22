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
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (UnauthorizedException ex)
        {
            _logger.LogWarning(ex, "Forbidden: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.Forbidden, ex.Message);
        }
        catch (NotFoundException ex)
        {
            _logger.LogInformation(ex, "Not found: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.NotFound, ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogInformation(ex, "Validation error: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Errors.ToArray());
        }
        catch (ArgumentException ex)
        {
            _logger.LogInformation(ex, "Argument error: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, ex.Message);
        }
        catch (FluentValidation.ValidationException ex)
        {
            _logger.LogInformation(ex, "FluentValidation error: {Message}", ex.Message);
            var errors = ex.Errors.Select(e => e.ErrorMessage).ToArray();
            await WriteErrorResponse(context, HttpStatusCode.BadRequest, errors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorResponse(context, HttpStatusCode.InternalServerError, "Error en el proceso");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, HttpStatusCode statusCode, params string[] errors)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ApiResponse<object>
        {
            Data = null,
            Success = false,
            Errors = errors.ToList()
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}
