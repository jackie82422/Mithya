using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace MockServer.Api.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
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
        catch (BadHttpRequestException ex)
        {
            // Invalid JSON body, malformed request, etc.
            _logger.LogWarning(ex, "Bad request on {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "BadRequest",
                message = "Invalid request body. Please ensure the JSON is well-formed."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "JSON parse error on {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "BadRequest",
                message = "Invalid JSON format. Please check your request body."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                return;

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new
            {
                error = "InternalServerError",
                message = "An unexpected error occurred. Please try again later."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
