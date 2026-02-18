using ManufacturerService.Models;

namespace ManufacturerService.Repositories;

/// <summary>
/// Repository interface for Stock operations
/// </summary>
public interface IStockRepository
{
    Task<Stock?> GetByBlanketIdAsync(int blanketId);
    Task<IEnumerable<Stock>> GetAllAsync();
    Task<Stock> AddAsync(Stock stock);
    Task<Stock> UpdateAsync(Stock stock);
    Task<bool> ReserveStockAsync(int blanketId, int quantity);
    Task<bool> ReleaseReservedStockAsync(int blanketId, int quantity);
    Task<bool> IncreaseStockAsync(int blanketId, int quantity);
}
