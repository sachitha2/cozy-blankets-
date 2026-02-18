using ManufacturerService.Models;

namespace ManufacturerService.Repositories;

/// <summary>
/// Repository interface for Blanket operations
/// Follows Repository pattern for data access abstraction
/// </summary>
public interface IBlanketRepository
{
    Task<IEnumerable<Blanket>> GetAllAsync();
    Task<Blanket?> GetByIdAsync(int id);
    Task<Blanket?> GetByModelNameAsync(string modelName);
    Task<Blanket> AddAsync(Blanket blanket);
    Task<Blanket> UpdateAsync(Blanket blanket);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
