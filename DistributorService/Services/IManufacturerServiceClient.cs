using DistributorService.DTOs;

namespace DistributorService.Services;

/// <summary>
/// HTTP client interface for communicating with ManufacturerService
/// Implements loose coupling between services
/// </summary>
public interface IManufacturerServiceClient
{
    Task<ManufacturerStockDto?> GetStockAsync(int blanketId);
    Task<ManufacturerProductionResponseDto> CheckProductionAsync(int blanketId, int quantity);
}
