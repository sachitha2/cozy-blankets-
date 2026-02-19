namespace DistributorService.Middleware;

/// <summary>
/// Optional API key authentication. When ApiKey:Enabled is true, requires X-Api-Key header to match ApiKey:Key.
/// Health and Swagger endpoints are excluded.
/// </summary>
public class ApiKeyMiddleware
{
    private const string ApiKeyHeader = "X-Api-Key";
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var enabled = _configuration.GetValue<bool>("ApiKey:Enabled");
        if (!enabled)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase) ||
            path == "/" || path == "")
        {
            await _next(context);
            return;
        }

        var expectedKey = _configuration["ApiKey:Key"];
        if (string.IsNullOrEmpty(expectedKey))
        {
            await _next(context);
            return;
        }

        var providedKey = context.Request.Headers[ApiKeyHeader].FirstOrDefault();
        if (string.IsNullOrEmpty(providedKey) || providedKey != expectedKey)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or missing API key. Provide X-Api-Key header." });
            return;
        }

        await _next(context);
    }
}
