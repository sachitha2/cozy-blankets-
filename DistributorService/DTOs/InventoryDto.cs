namespace DistributorService.DTOs;

/// <summary>
/// Data Transfer Object for Inventory information
/// </summary>
public class InventoryDto
{
    public int Id { get; set; }
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public DateTime LastUpdated { get; set; }
}
