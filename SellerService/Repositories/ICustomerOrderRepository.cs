using SellerService.Models;

namespace SellerService.Repositories;

/// <summary>
/// Repository interface for CustomerOrder operations
/// </summary>
public interface ICustomerOrderRepository
{
    Task<IEnumerable<CustomerOrder>> GetAllAsync();
    Task<CustomerOrder?> GetByIdAsync(int id);
    Task<CustomerOrder> AddAsync(CustomerOrder order);
    Task<CustomerOrder> UpdateAsync(CustomerOrder order);
}
