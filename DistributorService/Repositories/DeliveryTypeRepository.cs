using Microsoft.EntityFrameworkCore;
using DistributorService.Data;
using DistributorService.Models;

namespace DistributorService.Repositories;

/// <summary>
/// Repository implementation for DeliveryType operations
/// </summary>
public class DeliveryTypeRepository : IDeliveryTypeRepository
{
    private readonly DistributorDbContext _context;
    private readonly ILogger<DeliveryTypeRepository> _logger;

    public DeliveryTypeRepository(DistributorDbContext context, ILogger<DeliveryTypeRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<DeliveryType>> GetAllAsync()
    {
        try
        {
            return await _context.DeliveryTypes
                .Where(dt => dt.IsActive)
                .OrderBy(dt => dt.Name)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all delivery types");
            throw;
        }
    }

    public async Task<DeliveryType?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.DeliveryTypes.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery type with Id: {Id}", id);
            throw;
        }
    }

    public async Task<DeliveryType> AddAsync(DeliveryType deliveryType)
    {
        try
        {
            await _context.DeliveryTypes.AddAsync(deliveryType);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Delivery type added successfully with Id: {Id}", deliveryType.Id);
            return deliveryType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding delivery type");
            throw;
        }
    }

    public async Task<DeliveryType> UpdateAsync(DeliveryType deliveryType)
    {
        try
        {
            _context.DeliveryTypes.Update(deliveryType);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Delivery type updated successfully with Id: {Id}", deliveryType.Id);
            return deliveryType;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery type");
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var deliveryType = await GetByIdAsync(id);
            if (deliveryType == null)
            {
                return false;
            }

            // Soft delete by setting IsActive to false
            deliveryType.IsActive = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Delivery type deleted (soft delete) with Id: {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting delivery type");
            throw;
        }
    }
}
