namespace DistributorService.Models;

/// <summary>
/// Represents inventory items held by the distributor
/// </summary>
public class Inventory
{
    public int Id { get; set; }
    public int BlanketId { get; set; } // Reference to manufacturer's blanket model
    public string ModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; } = 0;
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public decimal UnitCost { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
