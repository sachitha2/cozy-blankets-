using SellerService.DTOs;

namespace SellerService.Services;

/// <summary>
/// HTTP client interface for communicating with DistributorService
/// Implements loose coupling between services
/// </summary>
public interface IDistributorServiceClient
{
    Task<DistributorOrderResponseDto> PlaceOrderAsync(string sellerId, int blanketId, int quantity, string? notes = null);
    Task<AvailabilityResponseDto?> CheckAvailabilityAsync(int blanketId);
}
