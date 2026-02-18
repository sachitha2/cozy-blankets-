using Microsoft.EntityFrameworkCore;
using SellerService.Data;
using SellerService.Models;

namespace SellerService.Repositories;

/// <summary>
/// Repository for Seller's own inventory.
/// </summary>
public class SellerInventoryRepository : ISellerInventoryRepository
{
    private readonly SellerDbContext _context;
    private readonly ILogger<SellerInventoryRepository> _logger;

    public SellerInventoryRepository(SellerDbContext context, ILogger<SellerInventoryRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SellerInventory?> GetByBlanketIdAsync(int blanketId)
    {
        return await _context.SellerInventories
            .FirstOrDefaultAsync(i => i.BlanketId == blanketId);
    }

    public async Task<IEnumerable<SellerInventory>> GetAllAsync()
    {
        return await _context.SellerInventories.ToListAsync();
    }

    public async Task<SellerInventory> UpdateAsync(SellerInventory inventory)
    {
        inventory.LastUpdated = DateTime.UtcNow;
        _context.SellerInventories.Update(inventory);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Seller inventory updated for BlanketId: {BlanketId}", inventory.BlanketId);
        return inventory;
    }
}
