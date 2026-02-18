using SellerService.DTOs;

namespace SellerService.Services;

/// <summary>
/// Service interface for Seller business logic
/// </summary>
public interface ISellerService
{
    Task<CustomerOrderResponseDto> ProcessCustomerOrderAsync(CustomerOrderRequestDto request);
    Task<AvailabilityResponseDto> CheckAvailabilityAsync(int modelId);
    Task<IEnumerable<CustomerOrderDto>> GetAllCustomerOrdersAsync();
    Task<CustomerOrderDto?> GetCustomerOrderByIdAsync(int id);
}
