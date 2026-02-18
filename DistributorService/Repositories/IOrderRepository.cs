using DistributorService.Models;

namespace DistributorService.Repositories;

/// <summary>
/// Repository interface for Order operations
/// </summary>
public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllAsync();
    Task<Order?> GetByIdAsync(int id);
    Task<IEnumerable<Order>> GetBySellerIdAsync(string sellerId);
    Task<Order> AddAsync(Order order);
    Task<Order> UpdateAsync(Order order);
}
