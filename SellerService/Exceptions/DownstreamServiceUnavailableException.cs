namespace SellerService.Exceptions;

/// <summary>
/// Thrown when an upstream/downstream service (e.g. DistributorService) is unreachable.
/// Controllers use this to return 502 Bad Gateway.
/// </summary>
public class DownstreamServiceUnavailableException : Exception
{
    public DownstreamServiceUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
