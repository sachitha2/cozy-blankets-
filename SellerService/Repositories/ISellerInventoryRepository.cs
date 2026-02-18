using SellerService.Models;

namespace SellerService.Repositories;

/// <summary>
/// Repository for Seller's own inventory (seller checks own stock first per PDF).
/// </summary>
public interface ISellerInventoryRepository
{
    Task<SellerInventory?> GetByBlanketIdAsync(int blanketId);
    Task<IEnumerable<SellerInventory>> GetAllAsync();
    Task<SellerInventory> UpdateAsync(SellerInventory inventory);
}
