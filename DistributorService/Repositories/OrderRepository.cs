using Microsoft.EntityFrameworkCore;
using DistributorService.Data;
using DistributorService.Models;

namespace DistributorService.Repositories;

/// <summary>
/// Repository implementation for Order operations
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly DistributorDbContext _context;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(DistributorDbContext context, ILogger<OrderRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Order>> GetAllAsync()
    {
        try
        {
            return await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all orders");
            throw;
        }
    }

    public async Task<Order?> GetByIdAsync(int id)
    {
        try
        {
            return await _context.Orders.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order with Id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<Order>> GetBySellerIdAsync(string sellerId)
    {
        try
        {
            return await _context.Orders
                .Where(o => o.SellerId == sellerId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders for SellerId: {SellerId}", sellerId);
            throw;
        }
    }

    public async Task<Order> AddAsync(Order order)
    {
        try
        {
            order.OrderDate = DateTime.UtcNow;
            order.Status = "Pending";
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Order added successfully with Id: {Id}", order.Id);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding order");
            throw;
        }
    }

    public async Task<Order> UpdateAsync(Order order)
    {
        try
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Order updated successfully with Id: {Id}", order.Id);
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order");
            throw;
        }
    }
}
