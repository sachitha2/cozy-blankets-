using Xunit;
using FluentAssertions;
using Moq;
using DistributorService.Services;
using DistributorService.Repositories;
using DistributorService.Models;
using DistributorService.DTOs;

namespace DistributorService.Tests.Services;

public class DistributorServiceTests
{
    private readonly Mock<IInventoryRepository> _mockInventoryRepository;
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<IManufacturerServiceClient> _mockManufacturerClient;
    private readonly DistributorService.Services.DistributorService _distributorService;

    public DistributorServiceTests()
    {
        _mockInventoryRepository = new Mock<IInventoryRepository>();
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockManufacturerClient = new Mock<IManufacturerServiceClient>();
        _distributorService = new DistributorService.Services.DistributorService(
            _mockInventoryRepository.Object,
            _mockOrderRepository.Object,
            _mockManufacturerClient.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<DistributorService.Services.DistributorService>.Instance
        );
    }

    [Fact]
    public async Task GetInventoryAsync_ShouldReturnAllInventory()
    {
        // Arrange - AvailableQuantity is computed (Quantity - ReservedQuantity)
        var inventory = new List<Inventory>
        {
            new Inventory { Id = 1, BlanketId = 1, ModelName = "Test Blanket", Quantity = 50, ReservedQuantity = 5 }
        };

        _mockInventoryRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(inventory);

        // Act
        var result = await _distributorService.GetInventoryAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task ProcessOrderAsync_WhenStockAvailable_ShouldFulfillOrder()
    {
        // Arrange
        var request = new OrderRequestDto
        {
            SellerId = "Seller-001",
            BlanketId = 1,
            Quantity = 10
        };

        var inventory = new Inventory
        {
            Id = 1,
            BlanketId = 1,
            ModelName = "Test Blanket",
            Quantity = 50,
            ReservedQuantity = 5,
            UnitCost = 35.00m
        };

        _mockInventoryRepository.Setup(r => r.GetByBlanketIdAsync(1))
            .ReturnsAsync(inventory);

        var order = new Order
        {
            Id = 1,
            SellerId = "Seller-001",
            BlanketId = 1,
            Quantity = 10,
            Status = "Fulfilled"
        };

        _mockOrderRepository.Setup(r => r.AddAsync(It.IsAny<Order>()))
            .ReturnsAsync(order);

        // Act
        var result = await _distributorService.ProcessOrderAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Status.Should().Be("Fulfilled");
        result.FulfilledFromStock.Should().BeTrue();
    }
}
