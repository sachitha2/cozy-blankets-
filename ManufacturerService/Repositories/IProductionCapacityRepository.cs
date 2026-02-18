using ManufacturerService.Models;

namespace ManufacturerService.Repositories;

/// <summary>
/// Repository interface for ProductionCapacity operations
/// </summary>
public interface IProductionCapacityRepository
{
    Task<ProductionCapacity?> GetByBlanketIdAsync(int blanketId);
    Task<IEnumerable<ProductionCapacity>> GetAllAsync();
    Task<ProductionCapacity> AddAsync(ProductionCapacity capacity);
    Task<ProductionCapacity> UpdateAsync(ProductionCapacity capacity);
    Task<bool> ExistsAsync(int blanketId);
}
