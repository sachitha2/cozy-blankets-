using ManufacturerService.Models;

namespace ManufacturerService.Repositories;

/// <summary>
/// Repository interface for ProductionOrder operations
/// </summary>
public interface IProductionOrderRepository
{
    Task<ProductionOrder?> GetByIdAsync(int id);
    Task<ProductionOrder?> GetByExternalOrderIdAsync(int externalOrderId);
    Task<IEnumerable<ProductionOrder>> GetByStatusAsync(string status);
    Task<ProductionOrder> AddAsync(ProductionOrder order);
    Task<ProductionOrder> UpdateAsync(ProductionOrder order);
}
