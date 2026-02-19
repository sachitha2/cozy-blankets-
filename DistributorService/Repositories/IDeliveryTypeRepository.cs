using DistributorService.Models;

namespace DistributorService.Repositories;

/// <summary>
/// Repository interface for DeliveryType operations
/// </summary>
public interface IDeliveryTypeRepository
{
    Task<IEnumerable<DeliveryType>> GetAllAsync();
    Task<DeliveryType?> GetByIdAsync(int id);
    Task<DeliveryType> AddAsync(DeliveryType deliveryType);
    Task<DeliveryType> UpdateAsync(DeliveryType deliveryType);
    Task<bool> DeleteAsync(int id);
}
