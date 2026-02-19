using Microsoft.EntityFrameworkCore;
using SellerService.Data;
using SellerService.Models;

namespace SellerService.Repositories;

/// <summary>
/// Repository implementation for CustomerOrder operations
/// </summary>
public class CustomerOrderRepository : ICustomerOrderRepository
{
    private readonly SellerDbContext _context;
    private readonly ILogger<CustomerOrderRepository> _logger;

    public CustomerOrderRepository(SellerDbContext context, ILogger<CustomerOrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<CustomerOrder>> GetAllAsync()
    {
        try
        {
            return await _context.CustomerOrders
                .Include(o => o.OrderItems)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all customer orders");
            throw;
        }
    }

    public async Task<IEnumerable<CustomerOrder>> GetByCustomerEmailAsync(string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            return Array.Empty<CustomerOrder>();
        try
        {
            var email = customerEmail.Trim();
            return await _context.CustomerOrders
                .Include(o => o.OrderItems)
                .Where(o => o.CustomerEmail == email)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer orders for email");
            throw;
        }
    }

    public async Task<CustomerOrder?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.CustomerOrders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer order with Id: {Id}", id);
            throw;
        }
    }

    public async Task<CustomerOrder> AddAsync(CustomerOrder order)
    {
        try
        {
            order.OrderDate = DateTime.UtcNow;
            if (string.IsNullOrEmpty(order.Status)) order.Status = "Pending";
            await _context.CustomerOrders.AddAsync(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Customer order added successfully with Id: {Id}", order.Id);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding customer order");
            throw;
        }
    }

    public async Task<CustomerOrder> UpdateAsync(CustomerOrder order)
    {
        try
        {
            _context.CustomerOrders.Update(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Customer order updated successfully with Id: {Id}", order.Id);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer order");
            throw;
        }
    }
}
