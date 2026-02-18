namespace ManufacturerService.Models;

/// <summary>
/// Represents production capacity and lead time information for a blanket model
/// </summary>
public class ProductionCapacity
{
    public int Id { get; set; }
    public int BlanketId { get; set; }
    public int DailyCapacity { get; set; } // Units that can be produced per day
    public int LeadTimeDays { get; set; } // Minimum days required for production
    public bool IsActive { get; set; } = true;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public Blanket Blanket { get; set; } = null!;
}
