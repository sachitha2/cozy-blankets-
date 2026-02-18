using Microsoft.EntityFrameworkCore;
using ManufacturerService.Data;
using ManufacturerService.Models;

namespace ManufacturerService.Repositories;

/// <summary>
/// Repository implementation for Stock operations
/// </summary>
public class StockRepository : IStockRepository
{
    private readonly ManufacturerDbContext _context;
    private readonly ILogger<StockRepository> _logger;

    public StockRepository(ManufacturerDbContext context, ILogger<StockRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Stock?> GetByBlanketIdAsync(int blanketId)
    {
        try
        {
            return await _context.Stocks
                .Include(s => s.Blanket)
                .FirstOrDefaultAsync(s => s.BlanketId == blanketId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock for BlanketId: {BlanketId}", blanketId);
            throw;
        }
    }

    public async Task<IEnumerable<Stock>> GetAllAsync()
    {
        try
        {
            return await _context.Stocks
                .Include(s => s.Blanket)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all stocks");
            throw;
        }
    }

    public async Task<Stock> AddAsync(Stock stock)
    {
        try
        {
            stock.LastUpdated = DateTime.UtcNow;
            await _context.Stocks.AddAsync(stock);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Stock added successfully for BlanketId: {BlanketId}", stock.BlanketId);
            return stock;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding stock");
            throw;
        }
    }

    public async Task<Stock> UpdateAsync(Stock stock)
    {
        try
        {
            stock.LastUpdated = DateTime.UtcNow;
            _context.Stocks.Update(stock);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Stock updated successfully for BlanketId: {BlanketId}", stock.BlanketId);
            return stock;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for BlanketId: {BlanketId}", stock.BlanketId);
            throw;
        }
    }

    public async Task<bool> ReserveStockAsync(int blanketId, int quantity)
    {
        try
        {
            var stock = await GetByBlanketIdAsync(blanketId);
            if (stock == null || stock.AvailableQuantity < quantity)
            {
                return false;
            }

            stock.ReservedQuantity += quantity;
            stock.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Stock reserved: {Quantity} units for BlanketId: {BlanketId}", quantity, blanketId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reserving stock for BlanketId: {BlanketId}", blanketId);
            throw;
        }
    }

    public async Task<bool> ReleaseReservedStockAsync(int blanketId, int quantity)
    {
        try
        {
            var stock = await GetByBlanketIdAsync(blanketId);
            if (stock == null || stock.ReservedQuantity < quantity)
            {
                return false;
            }

            stock.ReservedQuantity -= quantity;
            stock.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Reserved stock released: {Quantity} units for BlanketId: {BlanketId}", quantity, blanketId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing reserved stock for BlanketId: {BlanketId}", blanketId);
            throw;
        }
    }

    public async Task<bool> IncreaseStockAsync(int blanketId, int quantity)
    {
        try
        {
            var stock = await GetByBlanketIdAsync(blanketId);
            if (stock == null)
            {
                return false;
            }

            stock.Quantity += quantity;
            stock.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Stock increased: {Quantity} units for BlanketId: {BlanketId}", quantity, blanketId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error increasing stock for BlanketId: {BlanketId}", blanketId);
            throw;
        }
    }
}
