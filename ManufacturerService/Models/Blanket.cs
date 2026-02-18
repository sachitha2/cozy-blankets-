namespace ManufacturerService.Models;

/// <summary>
/// Represents a blanket model/product in the manufacturer's catalog
/// </summary>
public class Blanket
{
    public int Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Stock? Stock { get; set; }
    public ProductionCapacity? ProductionCapacity { get; set; }
}
