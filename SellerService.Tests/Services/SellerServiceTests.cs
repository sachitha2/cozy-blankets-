using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Configuration;
using SellerService.Services;
using SellerService.Repositories;
using SellerService.Models;
using SellerService.DTOs;

namespace SellerService.Tests.Services;

public class SellerServiceTests
{
    private readonly Mock<ICustomerOrderRepository> _mockOrderRepository;
    private readonly Mock<ISellerInventoryRepository> _mockSellerInventoryRepository;
    private readonly Mock<IDistributorServiceClient> _mockDistributorClient;
    private readonly SellerService.Services.SellerService _sellerService;

    public SellerServiceTests()
    {
        _mockOrderRepository = new Mock<ICustomerOrderRepository>();
        _mockSellerInventoryRepository = new Mock<ISellerInventoryRepository>();
        _mockDistributorClient = new Mock<IDistributorServiceClient>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Seller:Id"] = "Seller-001" })
            .Build();
        _sellerService = new SellerService.Services.SellerService(
            _mockOrderRepository.Object,
            _mockSellerInventoryRepository.Object,
            _mockDistributorClient.Object,
            configuration,
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

        var distributorResponse = new DistributorOrderResponseDto
        {
            OrderId = 1,
            Status = "Fulfilled",
            Message = "Order fulfilled",
            FulfilledFromStock = true
        };

        _mockDistributorClient.Setup(c => c.PlaceOrderAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()))
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
        var distributorResponse = new AvailabilityResponseDto
        {
            BlanketId = 1,
            ModelName = "Test Blanket",
            IsAvailable = true,
            AvailableQuantity = 50,
            Message = "In stock"
        };

        _mockDistributorClient.Setup(c => c.CheckAvailabilityAsync(1))
            .ReturnsAsync(distributorResponse);

        // Act
        var result = await _sellerService.CheckAvailabilityAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.IsAvailable.Should().BeTrue();
        result.AvailableQuantity.Should().Be(50);
    }
}
