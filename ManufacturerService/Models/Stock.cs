namespace ManufacturerService.Models;

/// <summary>
/// Represents the current stock level for a blanket model
/// </summary>
public class Stock
{
    public int Id { get; set; }
    public int BlanketId { get; set; }
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; } = 0;
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public Blanket Blanket { get; set; } = null!;
}
