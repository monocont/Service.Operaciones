using System.Text.Json;
using Service.Operaciones.Application.Common;

namespace Service.Operaciones.API.Middleware;

public class ResponseFormattingMiddleware
{
    private readonly RequestDelegate _next;

    public ResponseFormattingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;

        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
                return;

            if (context.Response.ContentType is not null &&
                !context.Response.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                return;

            responseBody.Seek(0, SeekOrigin.Begin);
            var body = await new StreamReader(responseBody).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                if (context.Response.StatusCode == 204)
                {
                    context.Response.StatusCode = 200;
                    var emptyApiResponse = ApiResponse<object>.OkNull();
                    var responseJson = JsonSerializer.Serialize(emptyApiResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    context.Response.Body = originalBodyStream;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(responseJson);
                    return;
                }
                return;
            }

            object? data;
            try
            {
                data = JsonSerializer.Deserialize<object>(body);
            }
            catch
            {
                data = body;
            }

            if (data is null)
                return;

            var messages = context.Items.TryGetValue("ResponseMessages", out var msgObj) && msgObj is List<string> msgList
                ? msgList
                : [];

            var wrapped = ApiResponse<object>.Ok(data, messages);
            var json = JsonSerializer.Serialize(wrapped, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            context.Response.Body = originalBodyStream;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(json);
        }
        finally
        {
            if (context.Response.Body == responseBody)
                context.Response.Body = originalBodyStream;
            responseBody.Dispose();
        }
    }
}
