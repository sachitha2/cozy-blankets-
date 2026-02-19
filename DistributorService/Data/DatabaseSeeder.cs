using DistributorService.Models;
using Microsoft.EntityFrameworkCore;

namespace DistributorService.Data;

/// <summary>
/// Seeds the database with example data
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Applies schema updates for existing databases that were created before
    /// DeliveryType and Order delivery columns were added (avoids "no such column" errors).
    /// </summary>
    private static void ApplySchemaUpdatesIfNeeded(DistributorDbContext context)
    {
        try
        {
            // Create DeliveryTypes table if it does not exist (SQLite)
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS DeliveryTypes (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT NOT NULL DEFAULT '',
                    Cost REAL NOT NULL DEFAULT 0,
                    EstimatedDays INTEGER NOT NULL DEFAULT 0,
                    IsActive INTEGER NOT NULL DEFAULT 1
                );");

            // Add new columns to Orders table if they don't exist (SQLite does not support IF NOT EXISTS for columns)
            try { context.Database.ExecuteSqlRaw(@"ALTER TABLE Orders ADD COLUMN DeliveryTypeId INTEGER REFERENCES DeliveryTypes(Id);"); } catch { /* column may already exist */ }
            try { context.Database.ExecuteSqlRaw(@"ALTER TABLE Orders ADD COLUMN DeliveryAddress TEXT;"); } catch { /* column may already exist */ }
        }
        catch
        {
            // Ignore; Migrate or EnsureCreated may have already applied the schema
        }
    }

    public static void SeedData(DistributorDbContext context)
    {
        // Ensure database is created
        context.Database.EnsureCreated();

        // Apply schema updates for existing DBs that lack DeliveryType and new Order columns
        ApplySchemaUpdatesIfNeeded(context);

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
