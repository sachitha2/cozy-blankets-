using ManufacturerService.Models;

namespace ManufacturerService.Data;

/// <summary>
/// Seeds the database with example data
/// </summary>
public static class DatabaseSeeder
{
    public static void SeedData(ManufacturerDbContext context)
    {
        // Ensure database is created
        context.Database.EnsureCreated();

        // Check if data already exists
        if (context.Blankets.Any())
        {
            return; // Data already seeded
        }

        // Seed Blankets
        var blankets = new List<Blanket>
        {
            new Blanket
            {
                ModelName = "Cozy Classic",
                Material = "100% Cotton",
                Description = "Soft and comfortable classic blanket perfect for all seasons",
                UnitPrice = 49.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Blanket
            {
                ModelName = "Warm Winter",
                Material = "Wool Blend",
                Description = "Extra warm blanket for cold winter nights",
                UnitPrice = 79.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Blanket
            {
                ModelName = "Luxury Plush",
                Material = "Premium Microfiber",
                Description = "Ultra-soft luxury blanket with premium feel",
                UnitPrice = 99.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Blanket
            {
                ModelName = "Light Breeze",
                Material = "Bamboo Fiber",
                Description = "Lightweight and breathable blanket for summer",
                UnitPrice = 59.99m,
                CreatedAt = DateTime.UtcNow
            },
            new Blanket
            {
                ModelName = "Family Size",
                Material = "Cotton Blend",
                Description = "Large blanket perfect for families",
                UnitPrice = 89.99m,
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Blankets.AddRange(blankets);
        context.SaveChanges();

        // Seed Stock
        var stocks = new List<Stock>
        {
            new Stock { BlanketId = 1, Quantity = 150, ReservedQuantity = 20, LastUpdated = DateTime.UtcNow },
            new Stock { BlanketId = 2, Quantity = 80, ReservedQuantity = 10, LastUpdated = DateTime.UtcNow },
            new Stock { BlanketId = 3, Quantity = 200, ReservedQuantity = 0, LastUpdated = DateTime.UtcNow },
            new Stock { BlanketId = 4, Quantity = 120, ReservedQuantity = 15, LastUpdated = DateTime.UtcNow },
            new Stock { BlanketId = 5, Quantity = 60, ReservedQuantity = 5, LastUpdated = DateTime.UtcNow }
        };

        context.Stocks.AddRange(stocks);
        context.SaveChanges();

        // Seed ProductionCapacity
        var capacities = new List<ProductionCapacity>
        {
            new ProductionCapacity { BlanketId = 1, DailyCapacity = 50, LeadTimeDays = 3, IsActive = true, LastUpdated = DateTime.UtcNow },
            new ProductionCapacity { BlanketId = 2, DailyCapacity = 30, LeadTimeDays = 5, IsActive = true, LastUpdated = DateTime.UtcNow },
            new ProductionCapacity { BlanketId = 3, DailyCapacity = 40, LeadTimeDays = 4, IsActive = true, LastUpdated = DateTime.UtcNow },
            new ProductionCapacity { BlanketId = 4, DailyCapacity = 60, LeadTimeDays = 2, IsActive = true, LastUpdated = DateTime.UtcNow },
            new ProductionCapacity { BlanketId = 5, DailyCapacity = 25, LeadTimeDays = 6, IsActive = true, LastUpdated = DateTime.UtcNow }
        };

        context.ProductionCapacities.AddRange(capacities);
        context.SaveChanges();
    }
}
