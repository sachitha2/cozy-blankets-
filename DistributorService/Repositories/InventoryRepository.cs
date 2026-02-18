using Microsoft.EntityFrameworkCore;
using DistributorService.Data;
using DistributorService.Models;

namespace DistributorService.Repositories;

/// <summary>
/// Repository implementation for Inventory operations
/// </summary>
public class InventoryRepository : IInventoryRepository
{
    private readonly DistributorDbContext _context;
    private readonly ILogger<InventoryRepository> _logger;

    public InventoryRepository(DistributorDbContext context, ILogger<InventoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Inventory>> GetAllAsync()
    {
        try
        {
            return await _context.Inventories.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all inventories");
            throw;
        }
    }

    public async Task<Inventory?> GetByBlanketIdAsync(int blanketId)
    {
        try
        {
            return await _context.Inventories
                .FirstOrDefaultAsync(i => i.BlanketId == blanketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory for BlanketId: {BlanketId}", blanketId);
            throw;
        }
    }

    public async Task<Inventory?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Inventories.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory with Id: {Id}", id);
            throw;
        }
    }

    public async Task<Inventory> AddAsync(Inventory inventory)
    {
        try
        {
            inventory.LastUpdated = DateTime.UtcNow;
            await _context.Inventories.AddAsync(inventory);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Inventory added successfully for BlanketId: {BlanketId}", inventory.BlanketId);
            return inventory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding inventory");
            throw;
        }
    }

    public async Task<Inventory> UpdateAsync(Inventory inventory)
    {
        try
        {
            inventory.LastUpdated = DateTime.UtcNow;
            _context.Inventories.Update(inventory);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Inventory updated successfully for BlanketId: {BlanketId}", inventory.BlanketId);
            return inventory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory");
            throw;
        }
    }

    public async Task<bool> ReserveInventoryAsync(int blanketId, int quantity)
    {
        try
        {
            var inventory = await GetByBlanketIdAsync(blanketId);
            if (inventory == null || inventory.AvailableQuantity < quantity)
            {
                return false;
            }

            inventory.ReservedQuantity += quantity;
            inventory.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Inventory reserved: {Quantity} units for BlanketId: {BlanketId}", quantity, blanketId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving inventory");
            throw;
        }
    }

    public async Task<bool> ReleaseReservedInventoryAsync(int blanketId, int quantity)
    {
        try
        {
            var inventory = await GetByBlanketIdAsync(blanketId);
            if (inventory == null || inventory.ReservedQuantity < quantity)
            {
                return false;
            }

            inventory.ReservedQuantity -= quantity;
            inventory.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Reserved inventory released: {Quantity} units for BlanketId: {BlanketId}", quantity, blanketId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing reserved inventory");
            throw;
        }
    }

    public async Task<bool> IncreaseInventoryAsync(int blanketId, int quantity)
    {
        try
        {
            var inventory = await GetByBlanketIdAsync(blanketId);
            if (inventory == null)
            {
                return false;
            }

            inventory.Quantity += quantity;
            inventory.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Inventory increased: {Quantity} units for BlanketId: {BlanketId}", quantity, blanketId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error increasing inventory");
            throw;
        }
    }
}
