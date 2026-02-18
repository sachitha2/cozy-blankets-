using DistributorService.DTOs;

namespace DistributorService.Services;

/// <summary>
/// Service interface for Distributor business logic
/// </summary>
public interface IDistributorService
{
    Task<IEnumerable<InventoryDto>> GetInventoryAsync();
    Task<OrderResponseDto> ProcessOrderAsync(OrderRequestDto request);
}
