using Xunit;
using FluentAssertions;
using Moq;
using SellerService.Services;
using SellerService.Repositories;
using SellerService.Models;
using SellerService.DTOs;

namespace SellerService.Tests.Services;

public class SellerServiceTests
{
    private readonly Mock<ICustomerOrderRepository> _mockOrderRepository;
    private readonly Mock<IDistributorServiceClient> _mockDistributorClient;
    private readonly SellerService.Services.SellerService _sellerService;

    public SellerServiceTests()
    {
        _mockOrderRepository = new Mock<ICustomerOrderRepository>();
        _mockDistributorClient = new Mock<IDistributorServiceClient>();
        _sellerService = new SellerService.Services.SellerService(
            _mockOrderRepository.Object,
            _mockDistributorClient.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SellerService.Services.SellerService>.Instance
        );
    }

    [Fact]
    public async Task ProcessCustomerOrderAsync_WhenDistributorHasStock_ShouldFulfillOrder()
    {
        // Arrange
        var request = new CustomerOrderRequestDto
        {
            CustomerName = "John Doe",
            CustomerEmail = "john@example.com",
            CustomerPhone = "123-456-7890",
            ShippingAddress = "123 Main St",
            Items = new List<OrderItemRequestDto>
            {
                new OrderItemRequestDto { BlanketId = 1, Quantity = 2 }
            }
        };

        var distributorResponse = new DistributorService.DTOs.OrderResponseDto
        {
            OrderId = 1,
            Status = "Fulfilled",
            Message = "Order fulfilled",
            FulfilledFromStock = true
        };

        _mockDistributorClient.Setup(c => c.PlaceOrderAsync(It.IsAny<DistributorService.DTOs.OrderRequestDto>()))
            .ReturnsAsync(distributorResponse);

        var order = new CustomerOrder
        {
            Id = 1,
            CustomerName = "John Doe",
            Status = "Fulfilled",
            TotalAmount = 99.98m
        };

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<CustomerOrder>()))
            .ReturnsAsync(order);

        // Act
        var result = await _sellerService.ProcessCustomerOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Fulfilled");
        result.OrderId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CheckAvailabilityAsync_WhenAvailable_ShouldReturnTrue()
    {
        // Arrange
        var distributorResponse = new DistributorService.DTOs.InventoryDto
        {
            Id = 1,
            BlanketId = 1,
            ModelName = "Test Blanket",
            AvailableQuantity = 50
        };

        _mockDistributorClient.Setup(c => c.GetInventoryByBlanketIdAsync(1))
            .ReturnsAsync(distributorResponse);

        // Act
        var result = await _sellerService.CheckAvailabilityAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.IsAvailable.Should().BeTrue();
        result.AvailableQuantity.Should().Be(50);
    }
}
