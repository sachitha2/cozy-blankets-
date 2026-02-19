using ManufacturerService.DTOs;

namespace ManufacturerService.Services;

/// <summary>
/// Service interface for Blanket business logic
/// Follows Service layer pattern for business logic abstraction
/// </summary>
public interface IBlanketService
{
    Task<IEnumerable<BlanketDto>> GetAllBlanketsAsync();
    Task<BlanketDto?> GetBlanketByIdAsync(int id);
    Task<StockDto?> GetStockByModelIdAsync(int modelId);
    Task<ProductionResponseDto> ProcessProductionRequestAsync(ProductionRequestDto request);
    Task<BlanketDto?> UpdateImageUrlAsync(int id, string? imageUrl);
    Task<BlanketDto?> AddAdditionalImageUrlAsync(int id, string imageUrl);
    
    // New methods for manufacturer dashboard
    Task<BlanketDto> CreateBlanketAsync(CreateBlanketRequest request);
    Task<BlanketDto?> UpdateBlanketAsync(int id, UpdateBlanketRequest request);
    Task<ProductionCapacityDto?> GetCapacityByBlanketIdAsync(int id);
    Task<ProductionCapacityDto?> UpdateCapacityAsync(int blanketId, UpdateProductionCapacityRequest request);
    Task<StockDto?> SetStockAsync(int blanketId, int quantity);
}
