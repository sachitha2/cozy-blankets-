using DistributorService.Models;

namespace DistributorService.Data;

/// <summary>
/// Seeds the database with example data
/// </summary>
public static class DatabaseSeeder
{
    public static void SeedData(DistributorDbContext context)
    {
        // Ensure database is created
        context.Database.EnsureCreated();

        // Check if data already exists
        if (context.Inventories.Any())
        {
            return; // Data already seeded
        }

        // Seed Inventory (matching ManufacturerService blanket IDs)
        var inventories = new List<Inventory>
        {
            new Inventory
            {
                BlanketId = 1,
                ModelName = "Cozy Classic",
                Quantity = 50,
                ReservedQuantity = 5,
                UnitCost = 35.00m,
                LastUpdated = DateTime.UtcNow
            },
            new Inventory
            {
                BlanketId = 2,
                ModelName = "Warm Winter",
                Quantity = 30,
                ReservedQuantity = 3,
                UnitCost = 55.00m,
                LastUpdated = DateTime.UtcNow
            },
            new Inventory
            {
                BlanketId = 3,
                ModelName = "Luxury Plush",
                Quantity = 75,
                ReservedQuantity = 0,
                UnitCost = 70.00m,
                LastUpdated = DateTime.UtcNow
            },
            new Inventory
            {
                BlanketId = 4,
                ModelName = "Light Breeze",
                Quantity = 40,
                ReservedQuantity = 8,
                UnitCost = 42.00m,
                LastUpdated = DateTime.UtcNow
            },
            new Inventory
            {
                BlanketId = 5,
                ModelName = "Family Size",
                Quantity = 25,
                ReservedQuantity = 2,
                UnitCost = 65.00m,
                LastUpdated = DateTime.UtcNow
            }
        };

        context.Inventories.AddRange(inventories);
        context.SaveChanges();
    }
}
