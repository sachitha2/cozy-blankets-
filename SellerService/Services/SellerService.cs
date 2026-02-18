using SellerService.DTOs;
using SellerService.Models;
using SellerService.Repositories;

namespace SellerService.Services;

/// <summary>
/// Service implementation for Seller business logic
/// Orchestrates customer order processing and distributor communication
/// </summary>
public class SellerService : ISellerService
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IDistributorServiceClient _distributorClient;
    private readonly ILogger<SellerService> _logger;
    private readonly string _sellerId;

    public SellerService(
        ICustomerOrderRepository orderRepository,
        IDistributorServiceClient distributorClient,
        IConfiguration configuration,
        ILogger<SellerService> logger)
    {
        _orderRepository = orderRepository;
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

            // Process each order item
            foreach (var itemRequest in request.Items)
            {
                // Check availability with distributor
                var availability = await _distributorClient.CheckAvailabilityAsync(itemRequest.BlanketId);
                
                var orderItem = new OrderItem
                {
                    BlanketId = itemRequest.BlanketId,
                    ModelName = availability?.ModelName ?? $"Model-{itemRequest.BlanketId}",
                    Quantity = itemRequest.Quantity,
                    UnitPrice = 0, // Will be set based on distributor pricing
                    Status = "Pending"
                };

                if (availability != null && availability.IsAvailable && availability.AvailableQuantity >= itemRequest.Quantity)
                {
                    // Place order with distributor
                    var distributorResponse = await _distributorClient.PlaceOrderAsync(
                        _sellerId,
                        itemRequest.BlanketId,
                        itemRequest.Quantity,
                        $"Customer order: {request.CustomerName}"
                    );

                    if (distributorResponse.Status == "Fulfilled")
                    {
                        orderItem.Status = "Fulfilled";
                        orderItem.UnitPrice = 79.99m; // Example pricing
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

    public async Task<AvailabilityResponseDto> CheckAvailabilityAsync(int modelId)
    {
        try
        {
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
}
