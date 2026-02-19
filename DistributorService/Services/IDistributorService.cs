using DistributorService.DTOs;

namespace DistributorService.Services;

/// <summary>
/// Service interface for Distributor business logic
/// </summary>
public interface IDistributorService
{
    // Inventory management
    Task<IEnumerable<InventoryDto>> GetInventoryAsync();
    Task<InventoryDto> AddInventoryAsync(CreateInventoryDto dto);
    Task<InventoryDto> UpdateInventoryAsync(int id, InventoryDto dto);
    Task<bool> DeleteInventoryAsync(int id);
    
    // Order management
    Task<IEnumerable<OrderDto>> GetOrdersAsync(string? status = null);
    Task<OrderDto?> GetOrderByIdAsync(int id);
    Task<OrderResponseDto> ProcessOrderAsync(OrderRequestDto request);
    Task<OrderDto> UpdateOrderStatusAsync(int id, string status);
    Task<OrderDto> UpdateOrderDeliveryAsync(int id, int? deliveryTypeId, string? deliveryAddress);
    Task<ReceiveFromManufacturerResponseDto> ReceiveFromManufacturerAsync(int orderId);
}
