using Microsoft.EntityFrameworkCore;
using ClientAppWeb.Models;

namespace ClientAppWeb.Data;

/// <summary>
/// DbContext for ClientAppWeb database
/// Stores contact form submissions
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ContactSubmission> ContactSubmissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ContactSubmission configuration
        modelBuilder.Entity<ContactSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Subject).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(5000);
            entity.Property(e => e.SubmittedAt).IsRequired();
            entity.Property(e => e.IsRead).IsRequired().HasDefaultValue(false);
        });
    }
}
