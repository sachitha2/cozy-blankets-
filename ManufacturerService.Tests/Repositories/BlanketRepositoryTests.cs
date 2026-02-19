using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ManufacturerService.Repositories;
using ManufacturerService.Models;
using ManufacturerService.Data;

namespace ManufacturerService.Tests.Repositories;

public class BlanketRepositoryTests : IDisposable
{
    private readonly ManufacturerDbContext _context;
    private readonly BlanketRepository _repository;

    public BlanketRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ManufacturerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ManufacturerDbContext(options);
        _repository = new BlanketRepository(_context, NullLogger<BlanketRepository>.Instance);
    }

    [Fact]
    public async Task AddAsync_ShouldAddBlanket()
    {
        // Arrange
        var blanket = new Blanket
        {
            ModelName = "Test Blanket",
            Material = "Cotton",
            Description = "Test Description",
            UnitPrice = 49.99m
        };

        // Act
        var result = await _repository.AddAsync(blanket);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        var savedBlanket = await _context.Blankets.FindAsync(result.Id);
        savedBlanket.Should().NotBeNull();
        savedBlanket!.ModelName.Should().Be("Test Blanket");
    }

    [Fact]
    public async Task GetByIdAsync_WhenExists_ShouldReturnBlanket()
    {
        // Arrange
        var blanket = new Blanket
        {
            ModelName = "Test Blanket",
            Material = "Cotton",
            UnitPrice = 49.99m
        };
        _context.Blankets.Add(blanket);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(blanket.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(blanket.Id);
        result.ModelName.Should().Be("Test Blanket");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllBlankets()
    {
        // Arrange
        _context.Blankets.AddRange(
            new Blanket { ModelName = "Blanket 1", Material = "Cotton", UnitPrice = 49.99m },
            new Blanket { ModelName = "Blanket 2", Material = "Wool", UnitPrice = 79.99m }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateBlanket()
    {
        // Arrange
        var blanket = new Blanket
        {
            ModelName = "Original Name",
            Material = "Cotton",
            UnitPrice = 49.99m
        };
        _context.Blankets.Add(blanket);
        await _context.SaveChangesAsync();

        blanket.ModelName = "Updated Name";
        blanket.UnitPrice = 59.99m;

        // Act
        var result = await _repository.UpdateAsync(blanket);

        // Assert
        result.ModelName.Should().Be("Updated Name");
        result.UnitPrice.Should().Be(59.99m);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
