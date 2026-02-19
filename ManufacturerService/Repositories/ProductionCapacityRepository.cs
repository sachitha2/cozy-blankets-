using Microsoft.EntityFrameworkCore;
using ManufacturerService.Data;
using ManufacturerService.Models;

namespace ManufacturerService.Repositories;

/// <summary>
/// Repository implementation for ProductionCapacity operations
/// </summary>
public class ProductionCapacityRepository : IProductionCapacityRepository
{
    private readonly ManufacturerDbContext _context;
    private readonly ILogger<ProductionCapacityRepository> _logger;

    public ProductionCapacityRepository(ManufacturerDbContext context, ILogger<ProductionCapacityRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductionCapacity?> GetByBlanketIdAsync(int blanketId)
    {
        try
        {
            return await _context.ProductionCapacities
                .Include(p => p.Blanket)
                .FirstOrDefaultAsync(p => p.BlanketId == blanketId && p.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving production capacity for BlanketId: {BlanketId}", blanketId);
            throw;
        }
    }

    public async Task<ProductionCapacity?> GetByBlanketIdIncludeInactiveAsync(int blanketId)
    {
        try
        {
            return await _context.ProductionCapacities
                .Include(p => p.Blanket)
                .FirstOrDefaultAsync(p => p.BlanketId == blanketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving production capacity for BlanketId: {BlanketId}", blanketId);
            throw;
        }
    }

    public async Task<IEnumerable<ProductionCapacity>> GetAllAsync()
    {
        try
        {
            return await _context.ProductionCapacities
                .Include(p => p.Blanket)
                .Where(p => p.IsActive)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all production capacities");
            throw;
        }
    }

    public async Task<ProductionCapacity> AddAsync(ProductionCapacity capacity)
    {
        try
        {
            capacity.LastUpdated = DateTime.UtcNow;
            await _context.ProductionCapacities.AddAsync(capacity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Production capacity added successfully for BlanketId: {BlanketId}", capacity.BlanketId);
            return capacity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding production capacity");
            throw;
        }
    }

    public async Task<ProductionCapacity> UpdateAsync(ProductionCapacity capacity)
    {
        try
        {
            capacity.LastUpdated = DateTime.UtcNow;
            _context.ProductionCapacities.Update(capacity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Production capacity updated successfully for BlanketId: {BlanketId}", capacity.BlanketId);
            return capacity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating production capacity for BlanketId: {BlanketId}", capacity.BlanketId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(int blanketId)
    {
        return await _context.ProductionCapacities.AnyAsync(p => p.BlanketId == blanketId && p.IsActive);
    }
}
