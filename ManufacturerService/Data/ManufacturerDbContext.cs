using Microsoft.EntityFrameworkCore;
using ManufacturerService.Models;

namespace ManufacturerService.Data;

/// <summary>
/// DbContext for ManufacturerService database
/// Implements Database per Service pattern
/// </summary>
public class ManufacturerDbContext : DbContext
{
    public ManufacturerDbContext(DbContextOptions<ManufacturerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Blanket> Blankets { get; set; }
    public DbSet<Stock> Stocks { get; set; }
    public DbSet<ProductionCapacity> ProductionCapacities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Blanket configuration
        modelBuilder.Entity<Blanket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Material).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.UnitPrice).HasColumnType("REAL");
            entity.Property(e => e.ImageUrl).HasMaxLength(500);
            entity.Property(e => e.AdditionalImageUrlsJson).HasMaxLength(2000);
            entity.HasIndex(e => e.ModelName).IsUnique();
        });

        // Stock configuration
        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.ReservedQuantity).IsRequired();
            entity.HasOne(e => e.Blanket)
                  .WithOne(e => e.Stock)
                  .HasForeignKey<Stock>(e => e.BlanketId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.BlanketId).IsUnique();
        });

        // ProductionCapacity configuration
        modelBuilder.Entity<ProductionCapacity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DailyCapacity).IsRequired();
            entity.Property(e => e.LeadTimeDays).IsRequired();
            entity.HasOne(e => e.Blanket)
                  .WithOne(e => e.ProductionCapacity)
                  .HasForeignKey<ProductionCapacity>(e => e.BlanketId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.BlanketId).IsUnique();
        });
    }
}
