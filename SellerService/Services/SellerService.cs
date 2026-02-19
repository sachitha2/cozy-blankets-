using SellerService.DTOs;
using SellerService.Models;
using SellerService.Repositories;

namespace SellerService.Services;

/// <summary>
/// Service implementation for Seller business logic.
/// PDF: "The Seller checks their own stock. If unavailable, they contact their assigned Distributor."
/// </summary>
public class SellerService : ISellerService
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly ISellerInventoryRepository _sellerInventoryRepository;
    private readonly IDistributorServiceClient _distributorClient;
    private readonly ILogger<SellerService> _logger;
    private readonly string _sellerId;

    public SellerService(
        ICustomerOrderRepository orderRepository,
        ISellerInventoryRepository sellerInventoryRepository,
        IDistributorServiceClient distributorClient,
        IConfiguration configuration,
        ILogger<SellerService> logger)
    {
        _orderRepository = orderRepository;
        _sellerInventoryRepository = sellerInventoryRepository;
        _distributorClient = distributorClient;
        _logger = logger;
        _sellerId = configuration["Seller:Id"] ?? "Seller-001";
    }

    public async Task<CustomerOrderResponseDto> ProcessCustomerOrderAsync(CustomerOrderRequestDto request)
    {
        try
        {
            if (request.Items == null || !request.Items.Any())
            {
                return new CustomerOrderResponseDto
                {
                    Status = "Error",
                    Message = "Order must contain at least one item"
                };
            }

            // Create customer order
            var customerOrder = new CustomerOrder
            {
                CustomerName = request.CustomerName,
                CustomerEmail = request.CustomerEmail,
                CustomerPhone = request.CustomerPhone,
                ShippingAddress = request.ShippingAddress,
                Status = "Processing",
                OrderItems = new List<OrderItem>()
            };

            var itemStatuses = new List<OrderItemStatusDto>();
            decimal totalAmount = 0;

            // Process each order item (PDF: Seller checks own stock first, then Distributor)
            foreach (var itemRequest in request.Items)
            {
                var orderItem = new OrderItem
                {
                    BlanketId = itemRequest.BlanketId,
                    ModelName = $"Model-{itemRequest.BlanketId}",
                    Quantity = itemRequest.Quantity,
                    UnitPrice = 0,
                    Status = "Pending"
                };

                // Step 1: Check Seller's own stock first (PDF: "The Seller checks their own stock")
                var sellerStock = await _sellerInventoryRepository.GetByBlanketIdAsync(itemRequest.BlanketId);
                if (sellerStock != null && sellerStock.AvailableQuantity >= itemRequest.Quantity)
                {
                    // Fulfill from seller's own inventory
                    orderItem.ModelName = sellerStock.ModelName;
                    orderItem.UnitPrice = 79.99m;
                    orderItem.Status = "Fulfilled";
                    sellerStock.Quantity -= itemRequest.Quantity;
                    await _sellerInventoryRepository.UpdateAsync(sellerStock);
                    var remainingAfter = sellerStock.AvailableQuantity - itemRequest.Quantity;
                    itemStatuses.Add(new OrderItemStatusDto
                    {
                        BlanketId = itemRequest.BlanketId,
                        ModelName = orderItem.ModelName,
                        Quantity = itemRequest.Quantity,
                        Status = "Fulfilled",
                        Message = $"Fulfilled from seller stock. {remainingAfter} units remaining."
                    });
                }
                else
                {
                    // Step 2: If unavailable, contact Distributor (PDF: "If unavailable, they contact their assigned Distributor")
                    var availability = await _distributorClient.CheckAvailabilityAsync(itemRequest.BlanketId);
                    orderItem.ModelName = availability?.ModelName ?? orderItem.ModelName;

                    if (availability != null && availability.IsAvailable && availability.AvailableQuantity >= itemRequest.Quantity)
                    {
                        var distributorResponse = await _distributorClient.PlaceOrderAsync(
                            _sellerId,
                            itemRequest.BlanketId,
                            itemRequest.Quantity,
                            $"Customer order: {request.CustomerName}"
                        );

                        if (distributorResponse.Status == "Fulfilled")
                        {
                            orderItem.Status = "Fulfilled";
                            orderItem.UnitPrice = 79.99m;
                            itemStatuses.Add(new OrderItemStatusDto
                            {
                                BlanketId = itemRequest.BlanketId,
                                ModelName = orderItem.ModelName,
                                Quantity = itemRequest.Quantity,
                                Status = "Fulfilled",
                                Message = distributorResponse.Message
                            });
                        }
                        else if (distributorResponse.Status == "PendingManufacturer")
                        {
                            orderItem.Status = "Processing";
                            orderItem.UnitPrice = 79.99m;
                            itemStatuses.Add(new OrderItemStatusDto
                            {
                                BlanketId = itemRequest.BlanketId,
                                ModelName = orderItem.ModelName,
                                Quantity = itemRequest.Quantity,
                                Status = "Processing",
                                Message = $"Order placed. Estimated delivery: {distributorResponse.EstimatedDeliveryDays} days"
                            });
                        }
                        else
                        {
                            orderItem.Status = "Unavailable";
                            itemStatuses.Add(new OrderItemStatusDto
                            {
                                BlanketId = itemRequest.BlanketId,
                                ModelName = orderItem.ModelName,
                                Quantity = itemRequest.Quantity,
                                Status = "Unavailable",
                                Message = distributorResponse.Message
                            });
                        }
                    }
                    else
                    {
                        orderItem.Status = "Unavailable";
                        itemStatuses.Add(new OrderItemStatusDto
                        {
                            BlanketId = itemRequest.BlanketId,
                            ModelName = orderItem.ModelName,
                            Quantity = itemRequest.Quantity,
                            Status = "Unavailable",
                            Message = availability?.Message ?? "Product not available"
                        });
                    }
                }

                customerOrder.OrderItems.Add(orderItem);
                totalAmount += orderItem.SubTotal;
            }

            customerOrder.TotalAmount = totalAmount;

            // Determine overall order status
            var allFulfilled = itemStatuses.All(i => i.Status == "Fulfilled");
            var anyUnavailable = itemStatuses.Any(i => i.Status == "Unavailable");
            var anyProcessing = itemStatuses.Any(i => i.Status == "Processing");

            if (allFulfilled)
            {
                customerOrder.Status = "Fulfilled";
                customerOrder.FulfilledDate = DateTime.UtcNow;
            }
            else if (anyUnavailable && !anyProcessing)
            {
                customerOrder.Status = "Cancelled";
            }
            else
            {
                customerOrder.Status = "Processing";
            }

            var savedOrder = await _orderRepository.AddAsync(customerOrder);

            _logger.LogInformation("Customer order {OrderId} processed. Status: {Status}", savedOrder.Id, savedOrder.Status);

            return new CustomerOrderResponseDto
            {
                OrderId = savedOrder.Id,
                Status = savedOrder.Status,
                Message = $"Order {savedOrder.Id} has been processed",
                Items = itemStatuses,
                TotalAmount = totalAmount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing customer order");
            throw;
        }
    }

    /// <summary>
    /// PDF: "Seller checks their own stock. If unavailable, they contact their assigned Distributor."
    /// </summary>
    public async Task<AvailabilityResponseDto> CheckAvailabilityAsync(int modelId)
    {
        try
        {
            // Step 1: Check Seller's own stock first
            var sellerStock = await _sellerInventoryRepository.GetByBlanketIdAsync(modelId);
            if (sellerStock != null && sellerStock.AvailableQuantity > 0)
            {
                return new AvailabilityResponseDto
                {
                    BlanketId = modelId,
                    ModelName = sellerStock.ModelName,
                    IsAvailable = true,
                    AvailableQuantity = sellerStock.AvailableQuantity,
                    Message = $"{sellerStock.AvailableQuantity} units available in seller stock"
                };
            }

            // Step 2: If unavailable, check with Distributor
            var availability = await _distributorClient.CheckAvailabilityAsync(modelId);
            if (availability == null)
            {
                return new AvailabilityResponseDto
                {
                    BlanketId = modelId,
                    IsAvailable = false,
                    AvailableQuantity = 0,
                    Message = "Unable to check availability"
                };
            }

            return availability;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for ModelId: {ModelId}", modelId);
            return new AvailabilityResponseDto
            {
                BlanketId = modelId,
                IsAvailable = false,
                AvailableQuantity = 0,
                Message = $"Error checking availability: {ex.Message}"
            };
        }
    }

    public async Task<IEnumerable<DTOs.CustomerOrderDto>> GetAllCustomerOrdersAsync()
    {
        try
        {
            var orders = await _orderRepository.GetAllAsync();
            return orders.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all customer orders");
            throw;
        }
    }

    public async Task<IEnumerable<DTOs.CustomerOrderDto>> GetCustomerOrdersByEmailAsync(string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            return Array.Empty<DTOs.CustomerOrderDto>();
        try
        {
            var orders = await _orderRepository.GetByCustomerEmailAsync(customerEmail);
            return orders.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer orders for email");
            throw;
        }
    }

    public async Task<DTOs.CustomerOrderDto?> GetCustomerOrderByIdAsync(int id)
    {
        try
        {
            var order = await _orderRepository.GetByIdAsync(id);
            return order != null ? MapToDto(order) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer order with Id: {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<SellerInventoryDto>> GetSellerInventoryAsync()
    {
        try
        {
            var inventory = await _sellerInventoryRepository.GetAllAsync();
            return inventory.Select(MapInventoryToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving seller inventory");
            throw;
        }
    }

    private static SellerInventoryDto MapInventoryToDto(Models.SellerInventory inventory)
    {
        return new SellerInventoryDto
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

    private static DTOs.CustomerOrderDto MapToDto(Models.CustomerOrder order)
    {
        return new DTOs.CustomerOrderDto
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            ShippingAddress = order.ShippingAddress,
            Status = order.Status,
            OrderDate = order.OrderDate,
            FulfilledDate = order.FulfilledDate,
            TotalAmount = order.TotalAmount,
            OrderItems = order.OrderItems.Select(item => new DTOs.OrderItemDto
            {
                Id = item.Id,
                BlanketId = item.BlanketId,
                ModelName = item.ModelName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                SubTotal = item.SubTotal,
                Status = item.Status
            }).ToList()
        };
    }
}
