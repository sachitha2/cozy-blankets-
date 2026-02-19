using ManufacturerService.DTOs;

namespace ManufacturerService.Services;

/// <summary>
/// Service interface for production order (backorder) operations
/// </summary>
public interface IProductionOrderService
{
    Task<ProductionOrderResponseDto?> CreateAsync(CreateProductionOrderRequestDto request);
    Task<ProductionOrderResponseDto?> GetByExternalOrderIdAsync(int externalOrderId);
    Task<ProductionOrderResponseDto?> CompleteAsync(int productionOrderId);
    Task<ProductionOrderResponseDto?> ShipAsync(int productionOrderId, int quantity);
}
