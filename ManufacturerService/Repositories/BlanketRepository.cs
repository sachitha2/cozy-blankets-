using Microsoft.EntityFrameworkCore;
using ManufacturerService.Data;
using ManufacturerService.Models;

namespace ManufacturerService.Repositories;

/// <summary>
/// Repository implementation for Blanket operations
/// Implements Repository pattern for data access
/// </summary>
public class BlanketRepository : IBlanketRepository
{
    private readonly ManufacturerDbContext _context;
    private readonly ILogger<BlanketRepository> _logger;

    public BlanketRepository(ManufacturerDbContext context, ILogger<BlanketRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Blanket>> GetAllAsync()
    {
        try
        {
            return await _context.Blankets
                .Include(b => b.Stock)
                .Include(b => b.ProductionCapacity)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all blankets");
            throw;
        }
    }

    public async Task<Blanket?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Blankets
                .Include(b => b.Stock)
                .Include(b => b.ProductionCapacity)
                .FirstOrDefaultAsync(b => b.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blanket with Id: {Id}", id);
            throw;
        }
    }

    public async Task<Blanket?> GetByModelNameAsync(string modelName)
    {
        try
        {
            return await _context.Blankets
                .Include(b => b.Stock)
                .Include(b => b.ProductionCapacity)
                .FirstOrDefaultAsync(b => b.ModelName == modelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blanket with ModelName: {ModelName}", modelName);
            throw;
        }
    }

    public async Task<Blanket> AddAsync(Blanket blanket)
    {
        try
        {
            blanket.CreatedAt = DateTime.UtcNow;
            await _context.Blankets.AddAsync(blanket);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Blanket added successfully with Id: {Id}", blanket.Id);
            return blanket;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding blanket");
            throw;
        }
    }

    public async Task<Blanket> UpdateAsync(Blanket blanket)
    {
        try
        {
            blanket.UpdatedAt = DateTime.UtcNow;
            _context.Blankets.Update(blanket);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Blanket updated successfully with Id: {Id}", blanket.Id);
            return blanket;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating blanket with Id: {Id}", blanket.Id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var blanket = await _context.Blankets.FindAsync(id);
            if (blanket == null)
            {
                return false;
            }

            _context.Blankets.Remove(blanket);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Blanket deleted successfully with Id: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blanket with Id: {Id}", id);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Blankets.AnyAsync(b => b.Id == id);
    }
}
