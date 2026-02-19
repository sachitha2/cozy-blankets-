using DistributorService.Models;

namespace DistributorService.Repositories;

/// <summary>
/// Repository interface for Inventory operations
/// </summary>
public interface IInventoryRepository
{
    Task<IEnumerable<Inventory>> GetAllAsync();
    Task<Inventory?> GetByBlanketIdAsync(int blanketId);
    Task<Inventory?> GetByIdAsync(int id);
    Task<Inventory> AddAsync(Inventory inventory);
    Task<Inventory> UpdateAsync(Inventory inventory);
    Task<bool> DeleteAsync(int id);
    Task<bool> ReserveInventoryAsync(int blanketId, int quantity);
    Task<bool> ReleaseReservedInventoryAsync(int blanketId, int quantity);
    Task<bool> IncreaseInventoryAsync(int blanketId, int quantity);
}
