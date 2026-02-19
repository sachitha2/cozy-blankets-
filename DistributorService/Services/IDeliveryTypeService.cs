using DistributorService.DTOs;

namespace DistributorService.Services;

/// <summary>
/// Service interface for DeliveryType business logic
/// </summary>
public interface IDeliveryTypeService
{
    Task<IEnumerable<DeliveryTypeDto>> GetAllAsync();
    Task<DeliveryTypeDto?> GetByIdAsync(int id);
    Task<DeliveryTypeDto> CreateAsync(CreateDeliveryTypeDto dto);
    Task<DeliveryTypeDto> UpdateAsync(int id, UpdateDeliveryTypeDto dto);
    Task<bool> DeleteAsync(int id);
}
