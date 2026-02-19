using ManufacturerService.DTOs;
using ManufacturerService.Models;
using ManufacturerService.Repositories;

namespace ManufacturerService.Services;

/// <summary>
/// Service for committed production orders (backorder) and reverse fulfillment
/// </summary>
public class ProductionOrderService : IProductionOrderService
{
    private readonly IBlanketRepository _blanketRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IProductionCapacityRepository _productionCapacityRepository;
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly ILogger<ProductionOrderService> _logger;

    public ProductionOrderService(
        IBlanketRepository blanketRepository,
        IStockRepository stockRepository,
        IProductionCapacityRepository productionCapacityRepository,
        IProductionOrderRepository productionOrderRepository,
        ILogger<ProductionOrderService> logger)
    {
        _blanketRepository = blanketRepository;
        _stockRepository = stockRepository;
        _productionCapacityRepository = productionCapacityRepository;
        _productionOrderRepository = productionOrderRepository;
        _logger = logger;
    }

    public async Task<ProductionOrderResponseDto?> CreateAsync(CreateProductionOrderRequestDto request)
    {
        try
        {
            var blanket = await _blanketRepository.GetByIdAsync(request.BlanketId);
            if (blanket == null)
            {
                _logger.LogWarning("Create production order: blanket {BlanketId} not found", request.BlanketId);
                return null;
            }

            var capacity = await _productionCapacityRepository.GetByBlanketIdAsync(request.BlanketId);
            if (capacity == null)
            {
                _logger.LogWarning("Create production order: no capacity for blanket {BlanketId}", request.BlanketId);
                return null;
            }

            var productionOrder = new ProductionOrder
            {
                BlanketId = request.BlanketId,
                Quantity = request.Quantity,
                Status = "Pending",
                ExternalOrderId = request.ExternalOrderId,
                CreatedAt = DateTime.UtcNow
            };

            var saved = await _productionOrderRepository.AddAsync(productionOrder);
            _logger.LogInformation("Production order created: Id={Id}, BlanketId={BlanketId}, Quantity={Quantity}, ExternalOrderId={ExternalOrderId}",
                saved.Id, saved.BlanketId, saved.Quantity, saved.ExternalOrderId);

            return MapToDto(saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating production order for BlanketId: {BlanketId}", request.BlanketId);
            throw;
        }
    }

    public async Task<ProductionOrderResponseDto?> GetByExternalOrderIdAsync(int externalOrderId)
    {
        try
        {
            var order = await _productionOrderRepository.GetByExternalOrderIdAsync(externalOrderId);
            return order != null ? MapToDto(order) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting production order by ExternalOrderId: {ExternalOrderId}", externalOrderId);
            throw;
        }
    }

    public async Task<ProductionOrderResponseDto?> CompleteAsync(int productionOrderId)
    {
        try
        {
            var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
            if (order == null)
            {
                _logger.LogWarning("Complete production order: order {Id} not found", productionOrderId);
                return null;
            }

            if (order.Status != "Pending")
            {
                _logger.LogWarning("Complete production order: order {Id} has status {Status}", productionOrderId, order.Status);
                return null;
            }

            var stock = await _stockRepository.GetByBlanketIdAsync(order.BlanketId);
            if (stock == null)
            {
                _logger.LogWarning("Complete production order: no stock record for BlanketId {BlanketId}", order.BlanketId);
                return null;
            }

            order.Status = "Completed";
            order.CompletedAt = DateTime.UtcNow;
            await _productionOrderRepository.UpdateAsync(order);

            await _stockRepository.IncreaseStockAsync(order.BlanketId, order.Quantity);

            _logger.LogInformation("Production order completed: Id={Id}, added {Quantity} to stock", productionOrderId, order.Quantity);
            return MapToDto(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing production order {Id}", productionOrderId);
            throw;
        }
    }

    public async Task<ProductionOrderResponseDto?> ShipAsync(int productionOrderId, int quantity)
    {
        try
        {
            var order = await _productionOrderRepository.GetByIdAsync(productionOrderId);
            if (order == null)
            {
                _logger.LogWarning("Ship production order: order {Id} not found", productionOrderId);
                return null;
            }

            if (order.Status != "Completed")
            {
                _logger.LogWarning("Ship production order: order {Id} must be Completed, current status {Status}", productionOrderId, order.Status);
                return null;
            }

            if (quantity <= 0 || quantity > order.Quantity)
            {
                _logger.LogWarning("Ship production order: invalid quantity {Quantity} for order {Id}", quantity, productionOrderId);
                return null;
            }

            var decreased = await _stockRepository.DecreaseStockAsync(order.BlanketId, quantity);
            if (!decreased)
            {
                _logger.LogWarning("Ship production order: insufficient stock for BlanketId {BlanketId}", order.BlanketId);
                return null;
            }

            order.Status = "Shipped";
            order.ShippedAt = DateTime.UtcNow;
            await _productionOrderRepository.UpdateAsync(order);

            _logger.LogInformation("Production order shipped: Id={Id}, Quantity={Quantity}", productionOrderId, quantity);
            return MapToDto(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping production order {Id}", productionOrderId);
            throw;
        }
    }

    private static ProductionOrderResponseDto MapToDto(ProductionOrder order)
    {
        return new ProductionOrderResponseDto
        {
            Id = order.Id,
            BlanketId = order.BlanketId,
            ModelName = order.Blanket?.ModelName,
            Quantity = order.Quantity,
            Status = order.Status,
            ExternalOrderId = order.ExternalOrderId,
            CreatedAt = order.CreatedAt,
            CompletedAt = order.CompletedAt,
            ShippedAt = order.ShippedAt
        };
    }
}
