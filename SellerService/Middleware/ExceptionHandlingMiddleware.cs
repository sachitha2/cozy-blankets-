using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using SellerService.Exceptions;

namespace SellerService.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Returns RFC 7807-style problem details with consistent shape and status codes.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred. TraceId: {TraceId}", context.TraceIdentifier);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            ArgumentException or ArgumentNullException => (HttpStatusCode.BadRequest, "Bad request"),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Not found"),
            DownstreamServiceUnavailableException => (HttpStatusCode.BadGateway, "Service temporarily unavailable"),
            _ => (HttpStatusCode.InternalServerError, "An internal server error occurred")
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            type = "https://tools.ietf.org/html/rfc7807",
            title,
            status = context.Response.StatusCode,
            detail = exception.Message,
            traceId = context.TraceIdentifier
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}
