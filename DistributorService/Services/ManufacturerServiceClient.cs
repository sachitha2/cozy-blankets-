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

    public async Task<ManufacturerProductionOrderDto?> CreateProductionOrderAsync(int blanketId, int quantity, int externalOrderId)
    {
        try
        {
            var manufacturerServiceUrl = _configuration["ManufacturerService:BaseUrl"] 
                ?? "http://localhost:5001";

            var request = new { blanketId, quantity, externalOrderId };
            var response = await _httpClient.PostAsJsonAsync(
                $"{manufacturerServiceUrl}/api/productionorders", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ManufacturerProductionOrderDto>();
                _logger.LogInformation("Created production order for BlanketId: {BlanketId}, ExternalOrderId: {ExternalOrderId}", blanketId, externalOrderId);
                return result;
            }

            _logger.LogWarning("Create production order failed. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception creating production order at ManufacturerService");
            return null;
        }
    }

    public async Task<ManufacturerProductionOrderDto?> GetProductionOrderByExternalOrderIdAsync(int externalOrderId)
    {
        try
        {
            var manufacturerServiceUrl = _configuration["ManufacturerService:BaseUrl"] 
                ?? "http://localhost:5001";

            var response = await _httpClient.GetAsync(
                $"{manufacturerServiceUrl}/api/productionorders/by-external/{externalOrderId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ManufacturerProductionOrderDto>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            _logger.LogWarning("Get production order by external id failed. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting production order from ManufacturerService");
            return null;
        }
    }

    public async Task<ManufacturerProductionOrderDto?> ShipProductionOrderAsync(int productionOrderId, int quantity)
    {
        try
        {
            var manufacturerServiceUrl = _configuration["ManufacturerService:BaseUrl"] 
                ?? "http://localhost:5001";

            var request = new { quantity };
            var response = await _httpClient.PostAsJsonAsync(
                $"{manufacturerServiceUrl}/api/productionorders/{productionOrderId}/ship", request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ManufacturerProductionOrderDto>();
                _logger.LogInformation("Shipped production order {ProductionOrderId}, Quantity: {Quantity}", productionOrderId, quantity);
                return result;
            }

            _logger.LogWarning("Ship production order failed. Status: {StatusCode}", response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception shipping production order at ManufacturerService");
            return null;
        }
    }
}
