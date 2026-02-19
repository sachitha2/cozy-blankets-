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
    private readonly IDeliveryTypeRepository _deliveryTypeRepository;
    private readonly IManufacturerServiceClient _manufacturerClient;
    private readonly ILogger<DistributorService> _logger;

    public DistributorService(
        IInventoryRepository inventoryRepository,
        IOrderRepository orderRepository,
        IDeliveryTypeRepository deliveryTypeRepository,
        IManufacturerServiceClient manufacturerClient,
        ILogger<DistributorService> logger)
    {
        _inventoryRepository = inventoryRepository;
        _orderRepository = orderRepository;
        _deliveryTypeRepository = deliveryTypeRepository;
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

    public async Task<InventoryDto> AddInventoryAsync(CreateInventoryDto dto)
    {
        try
        {
            var inventory = new Inventory
            {
                BlanketId = dto.BlanketId,
                ModelName = dto.ModelName,
                Quantity = dto.Quantity,
                ReservedQuantity = 0,
                UnitCost = dto.UnitCost,
                LastUpdated = DateTime.UtcNow
            };

            var created = await _inventoryRepository.AddAsync(inventory);
            _logger.LogInformation("Inventory added for BlanketId: {BlanketId}", created.BlanketId);
            return MapToDto(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding inventory");
            throw;
        }
    }

    public async Task<InventoryDto> UpdateInventoryAsync(int id, InventoryDto dto)
    {
        try
        {
            var inventory = await _inventoryRepository.GetByIdAsync(id);
            if (inventory == null)
            {
                throw new KeyNotFoundException($"Inventory with Id {id} not found");
            }

            inventory.Quantity = dto.Quantity;
            inventory.UnitCost = dto.UnitCost;
            inventory.ModelName = dto.ModelName;

            var updated = await _inventoryRepository.UpdateAsync(inventory);
            _logger.LogInformation("Inventory updated for Id: {Id}", updated.Id);
            return MapToDto(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory with Id: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteInventoryAsync(int id)
    {
        try
        {
            var inventory = await _inventoryRepository.GetByIdAsync(id);
            if (inventory == null)
            {
                return false;
            }

            // Check if inventory has reserved quantity
            if (inventory.ReservedQuantity > 0)
            {
                throw new InvalidOperationException($"Cannot delete inventory with reserved quantity: {inventory.ReservedQuantity}");
            }

            var result = await _inventoryRepository.DeleteAsync(id);
            _logger.LogInformation("Inventory deleted with Id: {Id}", id);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory with Id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(string? status = null)
    {
        try
        {
            var orders = await _orderRepository.GetAllAsync();
            
            if (!string.IsNullOrWhiteSpace(status))
            {
                orders = orders.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            var orderDtos = new List<OrderDto>();
            foreach (var order in orders)
            {
                var orderDto = await MapToOrderDtoAsync(order);
                orderDtos.Add(orderDto);
            }

            return orderDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            throw;
        }
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                return null;
            }

            return await MapToOrderDtoAsync(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order with Id: {Id}", id);
            throw;
        }
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(int id, string status)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                throw new KeyNotFoundException($"Order with Id {id} not found");
            }

            // Validate status transition
            if (order.Status == "Cancelled" && status != "Cancelled")
            {
                throw new InvalidOperationException("Cannot change status of a cancelled order");
            }

            if (order.Status == "Fulfilled" && status != "Fulfilled")
            {
                throw new InvalidOperationException("Cannot change status of a fulfilled order");
            }

            order.Status = status;
            if (status == "Fulfilled" && order.FulfilledDate == null)
            {
                order.FulfilledDate = DateTime.UtcNow;
            }

            var updated = await _orderRepository.UpdateAsync(order);
            _logger.LogInformation("Order {OrderId} status updated to {Status}", updated.Id, status);
            return await MapToOrderDtoAsync(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for Id: {Id}", id);
            throw;
        }
    }

    public async Task<OrderDto> UpdateOrderDeliveryAsync(int id, int? deliveryTypeId, string? deliveryAddress)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(id);
            if (order == null)
            {
                throw new KeyNotFoundException($"Order with Id {id} not found");
            }

            if (deliveryTypeId.HasValue)
            {
                var deliveryType = await _deliveryTypeRepository.GetByIdAsync(deliveryTypeId.Value);
                if (deliveryType == null)
                {
                    throw new KeyNotFoundException($"Delivery type with Id {deliveryTypeId.Value} not found");
                }
            }

            order.DeliveryTypeId = deliveryTypeId;
            order.DeliveryAddress = deliveryAddress;

            var updated = await _orderRepository.UpdateAsync(order);
            _logger.LogInformation("Order {OrderId} delivery information updated", updated.Id);
            return await MapToOrderDtoAsync(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order delivery for Id: {Id}", id);
            throw;
        }
    }

    private async Task<OrderDto> MapToOrderDtoAsync(Order order)
    {
        var orderDto = new OrderDto
        {
            Id = order.Id,
            SellerId = order.SellerId,
            BlanketId = order.BlanketId,
            ModelName = order.ModelName,
            Quantity = order.Quantity,
            Status = order.Status,
            OrderDate = order.OrderDate,
            FulfilledDate = order.FulfilledDate,
            Notes = order.Notes,
            DeliveryTypeId = order.DeliveryTypeId,
            DeliveryAddress = order.DeliveryAddress
        };

        if (order.DeliveryTypeId.HasValue)
        {
            var deliveryType = await _deliveryTypeRepository.GetByIdAsync(order.DeliveryTypeId.Value);
            if (deliveryType != null)
            {
                orderDto.DeliveryTypeName = deliveryType.Name;
            }
        }

        return orderDto;
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
