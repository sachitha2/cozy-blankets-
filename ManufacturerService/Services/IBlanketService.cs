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
}
