using DistributorService.DTOs;
using DistributorService.Models;
using DistributorService.Repositories;

namespace DistributorService.Services;

/// <summary>
/// Service implementation for Distributor business logic
/// Orchestrates order processing and inventory management
/// </summary>
public class DistributorService : IDistributorService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IManufacturerServiceClient _manufacturerClient;
    private readonly ILogger<DistributorService> _logger;

    public DistributorService(
        IInventoryRepository inventoryRepository,
        IOrderRepository orderRepository,
        IManufacturerServiceClient manufacturerClient,
        ILogger<DistributorService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _orderRepository = orderRepository;
        _manufacturerClient = manufacturerClient;
        _logger = logger;
    }

    public async Task<IEnumerable<InventoryDto>> GetInventoryAsync()
    {
        try
        {
            var inventories = await _inventoryRepository.GetAllAsync();
            return inventories.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory");
            throw;
        }
    }

    public async Task<OrderResponseDto> ProcessOrderAsync(OrderRequestDto request)
    {
        try
        {
            // Check distributor inventory first
            var inventory = await _inventoryRepository.GetByBlanketIdAsync(request.BlanketId);
            
            // Create order record
            var order = new Order
            {
                SellerId = request.SellerId,
                BlanketId = request.BlanketId,
                ModelName = inventory?.ModelName ?? $"Model-{request.BlanketId}",
                Quantity = request.Quantity,
                Status = "Pending",
                Notes = request.Notes
            };

            // If distributor has sufficient stock
            if (inventory != null && inventory.AvailableQuantity >= request.Quantity)
            {
                // Reserve inventory
                var reserved = await _inventoryRepository.ReserveInventoryAsync(request.BlanketId, request.Quantity);
                if (reserved)
                {
                    order.Status = "Fulfilled";
                    order.FulfilledDate = DateTime.UtcNow;
                    
                    // Update inventory quantity (fulfillment)
                    inventory.Quantity -= request.Quantity;
                    inventory.ReservedQuantity -= request.Quantity;
                    await _inventoryRepository.UpdateAsync(inventory);

                    var savedOrder = await _orderRepository.AddAsync(order);
                    
                    _logger.LogInformation("Order {OrderId} fulfilled from distributor stock", savedOrder.Id);
                    
                    return new OrderResponseDto
                    {
                        OrderId = savedOrder.Id,
                        Status = "Fulfilled",
                        Message = $"Order fulfilled from distributor stock. {inventory.AvailableQuantity} units remaining.",
                        FulfilledFromStock = true,
                        RequiresManufacturerOrder = false
                    };
                }
            }

            // Stock not available - check with manufacturer
            _logger.LogInformation("Insufficient distributor stock. Checking with manufacturer for BlanketId: {BlanketId}", request.BlanketId);
            
            var manufacturerResponse = await _manufacturerClient.CheckProductionAsync(request.BlanketId, request.Quantity);
            
            if (manufacturerResponse.CanProduce)
            {
                order.Status = "PendingManufacturer";
                order.Notes = $"Order placed. Manufacturer lead time: {manufacturerResponse.LeadTimeDays} days. " + request.Notes;
                
                var savedOrder = await _orderRepository.AddAsync(order);
                
                var productionOrder = await _manufacturerClient.CreateProductionOrderAsync(request.BlanketId, request.Quantity, savedOrder.Id);
                if (productionOrder != null)
                {
                    _logger.LogInformation("Created production order {ProductionOrderId} at manufacturer for distributor order {OrderId}", productionOrder.Id, savedOrder.Id);
                }
                else
                {
                    _logger.LogWarning("Failed to create production order at manufacturer for order {OrderId}", savedOrder.Id);
                }
                
                _logger.LogInformation("Order {OrderId} requires manufacturer production. Lead time: {LeadTimeDays} days", 
                    savedOrder.Id, manufacturerResponse.LeadTimeDays);
                
                return new OrderResponseDto
                {
                    OrderId = savedOrder.Id,
                    Status = "PendingManufacturer",
                    Message = manufacturerResponse.Message,
                    FulfilledFromStock = false,
                    RequiresManufacturerOrder = true,
                    EstimatedDeliveryDays = manufacturerResponse.LeadTimeDays
                };
            }
            else
            {
                order.Status = "Cancelled";
                order.Notes = $"Order cancelled. Manufacturer cannot fulfill: {manufacturerResponse.Message}. " + request.Notes;
                
                var savedOrder = await _orderRepository.AddAsync(order);
                
                _logger.LogWarning("Order {OrderId} cancelled. Manufacturer cannot fulfill request", savedOrder.Id);
                
                return new OrderResponseDto
                {
                    OrderId = savedOrder.Id,
                    Status = "Cancelled",
                    Message = $"Cannot fulfill order. {manufacturerResponse.Message}",
                    FulfilledFromStock = false,
                    RequiresManufacturerOrder = false
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order");
            throw;
        }
    }

    public async Task<ReceiveFromManufacturerResponseDto> ReceiveFromManufacturerAsync(int orderId)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                return new ReceiveFromManufacturerResponseDto { Success = false, Message = "Order not found.", OrderId = orderId };
            }

            if (order.Status != "PendingManufacturer")
            {
                return new ReceiveFromManufacturerResponseDto
                {
                    Success = false,
                    Message = $"Order is not pending manufacturer (current status: {order.Status}).",
                    OrderId = orderId,
                    OrderStatus = order.Status
                };
            }

            var productionOrder = await _manufacturerClient.GetProductionOrderByExternalOrderIdAsync(orderId);
            if (productionOrder == null)
            {
                return new ReceiveFromManufacturerResponseDto { Success = false, Message = "Production order not found at manufacturer.", OrderId = orderId };
            }

            if (productionOrder.Status != "Completed")
            {
                return new ReceiveFromManufacturerResponseDto
                {
                    Success = false,
                    Message = $"Production not complete yet (status: {productionOrder.Status}). Complete the production order at the manufacturer first.",
                    OrderId = orderId
                };
            }

            var shipResult = await _manufacturerClient.ShipProductionOrderAsync(productionOrder.Id, order.Quantity);
            if (shipResult == null)
            {
                return new ReceiveFromManufacturerResponseDto { Success = false, Message = "Failed to ship from manufacturer (e.g. insufficient stock).", OrderId = orderId };
            }

            var inventory = await _inventoryRepository.GetByBlanketIdAsync(order.BlanketId);
            if (inventory == null)
            {
                var newInventory = new Inventory
                {
                    BlanketId = order.BlanketId,
                    ModelName = order.ModelName,
                    Quantity = order.Quantity,
                    ReservedQuantity = 0,
                    UnitCost = 0
                };
                await _inventoryRepository.AddAsync(newInventory);
            }
            else
            {
                await _inventoryRepository.IncreaseInventoryAsync(order.BlanketId, order.Quantity);
            }

            order.Status = "Fulfilled";
            order.FulfilledDate = DateTime.UtcNow;
            order.Notes = (order.Notes ?? "") + " [Received from manufacturer and fulfilled.]";
            await _orderRepository.UpdateAsync(order);

            _logger.LogInformation("Order {OrderId} received from manufacturer and fulfilled", orderId);

            return new ReceiveFromManufacturerResponseDto
            {
                Success = true,
                Message = $"Received {order.Quantity} units from manufacturer and fulfilled order.",
                OrderId = orderId,
                OrderStatus = "Fulfilled"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving from manufacturer for order {OrderId}", orderId);
            throw;
        }
    }

    private static InventoryDto MapToDto(Inventory inventory)
    {
        return new InventoryDto
        {
            Id = inventory.Id,
            BlanketId = inventory.BlanketId,
            ModelName = inventory.ModelName,
            Quantity = inventory.Quantity,
            ReservedQuantity = inventory.ReservedQuantity,
            AvailableQuantity = inventory.AvailableQuantity,
            UnitCost = inventory.UnitCost,
            LastUpdated = inventory.LastUpdated
        };
    }
}
