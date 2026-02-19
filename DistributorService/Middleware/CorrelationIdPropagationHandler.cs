namespace DistributorService.Middleware;

/// <summary>
/// Propagates X-Correlation-Id from the current request to outgoing HTTP calls for distributed tracing.
/// </summary>
public class CorrelationIdPropagationHandler : DelegatingHandler
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdPropagationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items["CorrelationId"]?.ToString();
        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.TryAddWithoutValidation(CorrelationIdHeader, correlationId);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
