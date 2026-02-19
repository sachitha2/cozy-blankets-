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
        if (context.Inventories.Any() && context.DeliveryTypes.Any())
        {
            return; // Data already seeded
        }

        // Seed DeliveryTypes if they don't exist
        if (!context.DeliveryTypes.Any())
        {
            var deliveryTypes = new List<DeliveryType>
            {
                new DeliveryType
                {
                    Name = "Standard",
                    Description = "Standard delivery (5-7 business days)",
                    Cost = 5.00m,
                    EstimatedDays = 6,
                    IsActive = true
                },
                new DeliveryType
                {
                    Name = "Express",
                    Description = "Express delivery (2-3 business days)",
                    Cost = 15.00m,
                    EstimatedDays = 3,
                    IsActive = true
                },
                new DeliveryType
                {
                    Name = "Overnight",
                    Description = "Overnight delivery (next business day)",
                    Cost = 25.00m,
                    EstimatedDays = 1,
                    IsActive = true
                }
            };

            context.DeliveryTypes.AddRange(deliveryTypes);
            context.SaveChanges();
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

        // Seed Inventory if it doesn't exist
        if (!context.Inventories.Any())
        {
            context.Inventories.AddRange(inventories);
            context.SaveChanges();
        }
    }
}
