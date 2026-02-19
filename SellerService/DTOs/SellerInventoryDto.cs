namespace SellerService.DTOs;

/// <summary>
/// Data Transfer Object for Seller's own inventory
/// </summary>
public class SellerInventoryDto
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
