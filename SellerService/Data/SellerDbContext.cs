using Microsoft.EntityFrameworkCore;
using SellerService.Models;

namespace SellerService.Data;

/// <summary>
/// DbContext for SellerService database
/// Implements Database per Service pattern
/// </summary>
public class SellerDbContext : DbContext
{
    public SellerDbContext(DbContextOptions<SellerDbContext> options)
        : base(options)
    {
    }

    public DbSet<CustomerOrder> CustomerOrders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<SellerInventory> SellerInventories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // CustomerOrder configuration
        modelBuilder.Entity<CustomerOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CustomerEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CustomerPhone).HasMaxLength(50);
            entity.Property(e => e.ShippingAddress).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TotalAmount).HasColumnType("REAL");
            entity.HasMany(e => e.OrderItems)
                  .WithOne(e => e.CustomerOrder)
                  .HasForeignKey(e => e.CustomerOrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // OrderItem configuration
        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Quantity).IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnType("REAL");
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
        });

        // SellerInventory configuration (Seller's own stock - PDF: "Seller checks their own stock")
        modelBuilder.Entity<SellerInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ModelName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UnitCost).HasColumnType("REAL");
        });
    }
}
