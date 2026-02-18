using System.Net.Http.Json;
using SellerService.DTOs;

namespace SellerService.Services;

/// <summary>
/// HTTP client implementation for communicating with DistributorService
/// </summary>
public class DistributorServiceClient : IDistributorServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DistributorServiceClient> _logger;
    private readonly IConfiguration _configuration;

    public DistributorServiceClient(
        HttpClient httpClient,
        ILogger<DistributorServiceClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<DistributorOrderResponseDto> PlaceOrderAsync(string sellerId, int blanketId, int quantity, string? notes = null)
    {
        try
        {
            var distributorServiceUrl = _configuration["DistributorService:BaseUrl"] 
                ?? "http://localhost:5002";
            
            var request = new
            {
                sellerId = sellerId,
                blanketId = blanketId,
                quantity = quantity,
                notes = notes
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{distributorServiceUrl}/api/order", 
                request);
            
            if (response.IsSuccessStatusCode)
            {
                var orderResponse = await response.Content.ReadFromJsonAsync<DistributorOrderResponseDto>();
                _logger.LogInformation("Successfully placed order with DistributorService for BlanketId: {BlanketId}", blanketId);
                return orderResponse ?? new DistributorOrderResponseDto 
                { 
                    Status = "Error", 
                    Message = "Invalid response from DistributorService" 
                };
            }

            _logger.LogError("Error placing order with DistributorService. Status: {StatusCode}", response.StatusCode);
            return new DistributorOrderResponseDto 
            { 
                Status = "Error", 
                Message = $"DistributorService returned status: {response.StatusCode}" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while placing order with DistributorService");
            return new DistributorOrderResponseDto 
            { 
                Status = "Error", 
                Message = $"Error communicating with DistributorService: {ex.Message}" 
            };
        }
    }

    public async Task<AvailabilityResponseDto?> CheckAvailabilityAsync(int blanketId)
    {
        try
        {
            var distributorServiceUrl = _configuration["DistributorService:BaseUrl"] 
                ?? "http://localhost:5002";
            
            // First check distributor inventory
            var inventoryResponse = await _httpClient.GetAsync($"{distributorServiceUrl}/api/inventory");
            
            if (inventoryResponse.IsSuccessStatusCode)
            {
                var inventories = await inventoryResponse.Content.ReadFromJsonAsync<List<DistributorInventoryDto>>();
                var inventory = inventories?.FirstOrDefault(i => i.BlanketId == blanketId);
                
                if (inventory != null)
                {
                    return new AvailabilityResponseDto
                    {
                        BlanketId = inventory.BlanketId,
                        ModelName = inventory.ModelName,
                        IsAvailable = inventory.AvailableQuantity > 0,
                        AvailableQuantity = inventory.AvailableQuantity,
                        Message = inventory.AvailableQuantity > 0 
                            ? $"{inventory.AvailableQuantity} units available in distributor stock"
                            : "No stock available at distributor"
                    };
                }
            }

            _logger.LogWarning("Inventory not found for BlanketId: {BlanketId}", blanketId);
            return new AvailabilityResponseDto
            {
                BlanketId = blanketId,
                IsAvailable = false,
                AvailableQuantity = 0,
                Message = "Product not found in distributor inventory"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while checking availability with DistributorService");
            return new AvailabilityResponseDto
            {
                BlanketId = blanketId,
                IsAvailable = false,
                AvailableQuantity = 0,
                Message = $"Error checking availability: {ex.Message}"
            };
        }
    }

    // Helper DTO for distributor inventory
    private class DistributorInventoryDto
    {
        public int BlanketId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
    }
}
