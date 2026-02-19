using DistributorService.DTOs;

namespace DistributorService.Services;

/// <summary>
/// HTTP client interface for communicating with ManufacturerService
/// Implements loose coupling between services
/// </summary>
public interface IManufacturerServiceClient
{
    Task<ManufacturerStockDto?> GetStockAsync(int blanketId);
    Task<ManufacturerProductionResponseDto> CheckProductionAsync(int blanketId, int quantity);
    Task<ManufacturerProductionOrderDto?> CreateProductionOrderAsync(int blanketId, int quantity, int externalOrderId);
    Task<ManufacturerProductionOrderDto?> GetProductionOrderByExternalOrderIdAsync(int externalOrderId);
    Task<ManufacturerProductionOrderDto?> ShipProductionOrderAsync(int productionOrderId, int quantity);
}
