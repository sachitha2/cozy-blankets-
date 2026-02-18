using System.Net.Http.Json;
using DistributorService.DTOs;

namespace DistributorService.Services;

/// <summary>
/// HTTP client implementation for communicating with ManufacturerService
/// </summary>
public class ManufacturerServiceClient : IManufacturerServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ManufacturerServiceClient> _logger;
    private readonly IConfiguration _configuration;

    public ManufacturerServiceClient(
        HttpClient httpClient,
        ILogger<ManufacturerServiceClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ManufacturerStockDto?> GetStockAsync(int blanketId)
    {
        try
        {
            var manufacturerServiceUrl = _configuration["ManufacturerService:BaseUrl"] 
                ?? "http://localhost:5001";
            
            var response = await _httpClient.GetAsync($"{manufacturerServiceUrl}/api/blankets/stock/{blanketId}");
            
            if (response.IsSuccessStatusCode)
            {
                var stock = await response.Content.ReadFromJsonAsync<ManufacturerStockDto>();
                _logger.LogInformation("Successfully retrieved stock for BlanketId: {BlanketId}", blanketId);
                return stock;
            }
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Stock not found for BlanketId: {BlanketId}", blanketId);
                return null;
            }

            _logger.LogError("Error retrieving stock from ManufacturerService. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while retrieving stock from ManufacturerService for BlanketId: {BlanketId}", blanketId);
            return null;
        }
    }

    public async Task<ManufacturerProductionResponseDto> CheckProductionAsync(int blanketId, int quantity)
    {
        try
        {
            var manufacturerServiceUrl = _configuration["ManufacturerService:BaseUrl"] 
                ?? "http://localhost:5001";
            
            var request = new
            {
                blanketId = blanketId,
                quantity = quantity
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{manufacturerServiceUrl}/api/blankets/produce", 
                request);
            
            if (response.IsSuccessStatusCode)
            {
                var productionResponse = await response.Content.ReadFromJsonAsync<ManufacturerProductionResponseDto>();
                _logger.LogInformation("Successfully checked production for BlanketId: {BlanketId}, Quantity: {Quantity}", 
                    blanketId, quantity);
                return productionResponse ?? new ManufacturerProductionResponseDto 
                { 
                    CanProduce = false, 
                    Message = "Invalid response from ManufacturerService" 
                };
            }

            _logger.LogError("Error checking production from ManufacturerService. Status: {StatusCode}", response.StatusCode);
            return new ManufacturerProductionResponseDto 
            { 
                CanProduce = false, 
                Message = $"ManufacturerService returned status: {response.StatusCode}" 
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while checking production from ManufacturerService");
            return new ManufacturerProductionResponseDto 
            { 
                CanProduce = false, 
                Message = $"Error communicating with ManufacturerService: {ex.Message}" 
            };
        }
    }
}
