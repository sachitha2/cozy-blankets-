using Microsoft.EntityFrameworkCore;
using ManufacturerService.Data;
using ManufacturerService.Models;

namespace ManufacturerService.Repositories;

/// <summary>
/// Repository implementation for ProductionOrder operations
/// </summary>
public class ProductionOrderRepository : IProductionOrderRepository
{
    private readonly ManufacturerDbContext _context;
    private readonly ILogger<ProductionOrderRepository> _logger;

    public ProductionOrderRepository(ManufacturerDbContext context, ILogger<ProductionOrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductionOrder?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.ProductionOrders
                .Include(p => p.Blanket)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving production order with Id: {Id}", id);
            throw;
        }
    }

    public async Task<ProductionOrder?> GetByExternalOrderIdAsync(int externalOrderId)
    {
        try
        {
            return await _context.ProductionOrders
                .Include(p => p.Blanket)
                .FirstOrDefaultAsync(p => p.ExternalOrderId == externalOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving production order by ExternalOrderId: {ExternalOrderId}", externalOrderId);
            throw;
        }
    }

    public async Task<IEnumerable<ProductionOrder>> GetByStatusAsync(string status)
    {
        try
        {
            return await _context.ProductionOrders
                .Include(p => p.Blanket)
                .Where(p => p.Status == status)
                .OrderBy(p => p.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving production orders by status: {Status}", status);
            throw;
        }
    }

    public async Task<ProductionOrder> AddAsync(ProductionOrder order)
    {
        try
        {
            await _context.ProductionOrders.AddAsync(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Production order added: Id={Id}, BlanketId={BlanketId}, Quantity={Quantity}",
                order.Id, order.BlanketId, order.Quantity);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding production order");
            throw;
        }
    }

    public async Task<ProductionOrder> UpdateAsync(ProductionOrder order)
    {
        try
        {
            _context.ProductionOrders.Update(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Production order updated: Id={Id}, Status={Status}", order.Id, order.Status);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating production order Id: {Id}", order.Id);
            throw;
        }
    }
}
