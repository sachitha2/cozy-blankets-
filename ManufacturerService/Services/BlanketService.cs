using System.Text.Json;
using ManufacturerService.DTOs;
using ManufacturerService.Models;
using ManufacturerService.Repositories;

namespace ManufacturerService.Services;

/// <summary>
/// Service implementation for Blanket business logic
/// Implements business rules and orchestrates repository calls
/// </summary>
public class BlanketService : IBlanketService
{
    private readonly IBlanketRepository _blanketRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IProductionCapacityRepository _productionCapacityRepository;
    private readonly ILogger<BlanketService> _logger;

    public BlanketService(
        IBlanketRepository blanketRepository,
        IStockRepository stockRepository,
        IProductionCapacityRepository productionCapacityRepository,
        ILogger<BlanketService> logger)
    {
        _blanketRepository = blanketRepository;
        _stockRepository = stockRepository;
        _productionCapacityRepository = productionCapacityRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<BlanketDto>> GetAllBlanketsAsync()
    {
        try
        {
            var blankets = await _blanketRepository.GetAllAsync();
            return blankets.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all blankets");
            throw;
        }
    }

    public async Task<BlanketDto?> GetBlanketByIdAsync(int id)
    {
        try
        {
            var blanket = await _blanketRepository.GetByIdAsync(id);
            return blanket != null ? MapToDto(blanket) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blanket with Id: {Id}", id);
            throw;
        }
    }

    public async Task<StockDto?> GetStockByModelIdAsync(int modelId)
    {
        try
        {
            var blanket = await _blanketRepository.GetByIdAsync(modelId);
            if (blanket == null)
            {
                _logger.LogWarning("Blanket with Id: {ModelId} not found", modelId);
                return null;
            }

            var stock = await _stockRepository.GetByBlanketIdAsync(modelId);
            if (stock == null)
            {
                _logger.LogWarning("Stock not found for BlanketId: {ModelId}", modelId);
                return null;
            }

            return new StockDto
            {
                BlanketId = stock.BlanketId,
                ModelName = blanket.ModelName,
                Quantity = stock.Quantity,
                ReservedQuantity = stock.ReservedQuantity,
                AvailableQuantity = stock.AvailableQuantity,
                LastUpdated = stock.LastUpdated
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock for ModelId: {ModelId}", modelId);
            throw;
        }
    }

    public async Task<BlanketDto?> UpdateImageUrlAsync(int id, string? imageUrl)
    {
        try
        {
            var blanket = await _blanketRepository.GetByIdAsync(id);
            if (blanket == null)
                return null;
            blanket.ImageUrl = imageUrl;
            blanket.UpdatedAt = DateTime.UtcNow;
            await _blanketRepository.UpdateAsync(blanket);
            return MapToDto(blanket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating image URL for blanket Id: {Id}", id);
            throw;
        }
    }

    public async Task<ProductionResponseDto> ProcessProductionRequestAsync(ProductionRequestDto request)
    {
        try
        {
            // Validate blanket exists
            var blanket = await _blanketRepository.GetByIdAsync(request.BlanketId);
            if (blanket == null)
            {
                return new ProductionResponseDto
                {
                    CanProduce = false,
                    Message = $"Blanket with Id {request.BlanketId} not found"
                };
            }

            // Check current stock
            var stock = await _stockRepository.GetByBlanketIdAsync(request.BlanketId);
            var availableStock = stock?.AvailableQuantity ?? 0;

            // If sufficient stock available, return immediately
            if (availableStock >= request.Quantity)
            {
                return new ProductionResponseDto
                {
                    CanProduce = true,
                    AvailableStock = availableStock,
                    LeadTimeDays = 0,
                    EstimatedCompletionDate = DateTime.UtcNow,
                    Message = $"Sufficient stock available. {availableStock} units in stock."
                };
            }

            // Check production capacity
            var capacity = await _productionCapacityRepository.GetByBlanketIdAsync(request.BlanketId);
            if (capacity == null)
            {
                return new ProductionResponseDto
                {
                    CanProduce = false,
                    AvailableStock = availableStock,
                    Message = $"Production capacity not configured for blanket model {blanket.ModelName}"
                };
            }

            // Calculate production time needed
            var unitsToProduce = request.Quantity - availableStock;
            var daysNeeded = (int)Math.Ceiling((double)unitsToProduce / capacity.DailyCapacity);
            var totalLeadTime = capacity.LeadTimeDays + daysNeeded;
            var estimatedCompletion = DateTime.UtcNow.AddDays(totalLeadTime);

            // Check if requested delivery date is feasible
            if (request.RequestedDeliveryDate.HasValue && estimatedCompletion > request.RequestedDeliveryDate.Value)
            {
                return new ProductionResponseDto
                {
                    CanProduce = false,
                    AvailableStock = availableStock,
                    LeadTimeDays = totalLeadTime,
                    EstimatedCompletionDate = estimatedCompletion,
                    Message = $"Cannot meet requested delivery date. Estimated completion: {estimatedCompletion:yyyy-MM-dd}"
                };
            }

            return new ProductionResponseDto
            {
                CanProduce = true,
                AvailableStock = availableStock,
                LeadTimeDays = totalLeadTime,
                EstimatedCompletionDate = estimatedCompletion,
                Message = $"Can produce {request.Quantity} units. {availableStock} available now, {unitsToProduce} need production. Estimated completion: {estimatedCompletion:yyyy-MM-dd}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing production request for BlanketId: {BlanketId}", request.BlanketId);
            throw;
        }
    }

    public async Task<BlanketDto?> AddAdditionalImageUrlAsync(int id, string imageUrl)
    {
        try
        {
            var blanket = await _blanketRepository.GetByIdAsync(id);
            if (blanket == null)
                return null;
            var list = DeserializeImageUrls(blanket.AdditionalImageUrlsJson);
            if (!string.IsNullOrWhiteSpace(imageUrl) && !list.Contains(imageUrl))
            {
                list.Add(imageUrl);
                blanket.AdditionalImageUrlsJson = JsonSerializer.Serialize(list);
                if (string.IsNullOrEmpty(blanket.ImageUrl))
                    blanket.ImageUrl = imageUrl;
                blanket.UpdatedAt = DateTime.UtcNow;
                await _blanketRepository.UpdateAsync(blanket);
            }
            return MapToDto(blanket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding additional image for blanket Id: {Id}", id);
            throw;
        }
    }

    private static List<string> DeserializeImageUrls(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch { return new List<string>(); }
    }

    private static BlanketDto MapToDto(Blanket blanket)
    {
        return new BlanketDto
        {
            Id = blanket.Id,
            ModelName = blanket.ModelName,
            Material = blanket.Material,
            Description = blanket.Description,
            UnitPrice = blanket.UnitPrice,
            ImageUrl = blanket.ImageUrl,
            AdditionalImageUrls = DeserializeImageUrls(blanket.AdditionalImageUrlsJson),
            CreatedAt = blanket.CreatedAt
        };
    }
}
