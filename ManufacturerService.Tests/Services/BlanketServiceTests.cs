using Xunit;
using FluentAssertions;
using Moq;
using ManufacturerService.Services;
using ManufacturerService.Repositories;
using ManufacturerService.Models;
using ManufacturerService.DTOs;

namespace ManufacturerService.Tests.Services;

public class BlanketServiceTests
{
    private readonly Mock<IBlanketRepository> _mockBlanketRepository;
    private readonly Mock<IStockRepository> _mockStockRepository;
    private readonly Mock<IProductionCapacityRepository> _mockProductionCapacityRepository;
    private readonly BlanketService _blanketService;

    public BlanketServiceTests()
    {
        _mockBlanketRepository = new Mock<IBlanketRepository>();
        _mockStockRepository = new Mock<IStockRepository>();
        _mockProductionCapacityRepository = new Mock<IProductionCapacityRepository>();
        _blanketService = new BlanketService(
            _mockBlanketRepository.Object,
            _mockStockRepository.Object,
            _mockProductionCapacityRepository.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<BlanketService>.Instance
        );
    }

    [Fact]
    public async Task GetAllBlanketsAsync_ShouldReturnAllBlankets()
    {
        // Arrange
        var blankets = new List<Blanket>
        {
            new Blanket { Id = 1, ModelName = "Test Blanket 1", Material = "Cotton", UnitPrice = 49.99m },
            new Blanket { Id = 2, ModelName = "Test Blanket 2", Material = "Wool", UnitPrice = 79.99m }
        };

        _mockBlanketRepository.Setup(r => r.GetAllAsync())
            .ReturnsAsync(blankets);

        // Act
        var result = await _blanketService.GetAllBlanketsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.First().ModelName.Should().Be("Test Blanket 1");
    }

    [Fact]
    public async Task GetBlanketByIdAsync_WhenExists_ShouldReturnBlanket()
    {
        // Arrange
        var blanket = new Blanket 
        { 
            Id = 1, 
            ModelName = "Test Blanket", 
            Material = "Cotton", 
            UnitPrice = 49.99m 
        };

        _mockBlanketRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(blanket);

        // Act
        var result = await _blanketService.GetBlanketByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.ModelName.Should().Be("Test Blanket");
    }

    [Fact]
    public async Task GetBlanketByIdAsync_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        _mockBlanketRepository.Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Blanket?)null);

        // Act
        var result = await _blanketService.GetBlanketByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStockByModelIdAsync_WhenExists_ShouldReturnStock()
    {
        // Arrange
        var stock = new Stock
        {
            Id = 1,
            BlanketId = 1,
            Quantity = 100,
            ReservedQuantity = 10,
            AvailableQuantity = 90
        };

        _mockStockRepository.Setup(r => r.GetByBlanketIdAsync(1))
            .ReturnsAsync(stock);

        var blanket = new Blanket { Id = 1, ModelName = "Test Blanket" };
        _mockBlanketRepository.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(blanket);

        // Act
        var result = await _blanketService.GetStockByModelIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.BlanketId.Should().Be(1);
        result.AvailableQuantity.Should().Be(90);
    }

    [Fact]
    public async Task ProcessProductionRequestAsync_WhenStockAvailable_ShouldReturnImmediateAvailability()
    {
        // Arrange
        var request = new ProductionRequestDto
        {
            BlanketId = 1,
            Quantity = 50
        };

        var stock = new Stock
        {
            Id = 1,
            BlanketId = 1,
            Quantity = 100,
            ReservedQuantity = 10,
            AvailableQuantity = 90
        };

        _mockStockRepository.Setup(r => r.GetByBlanketIdAsync(1))
            .ReturnsAsync(stock);

        // Act
        var result = await _blanketService.ProcessProductionRequestAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.CanProduce.Should().BeTrue();
        result.AvailableStock.Should().Be(90);
        result.LeadTimeDays.Should().Be(0);
    }
}
