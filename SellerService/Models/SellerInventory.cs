namespace SellerService.Models;

/// <summary>
/// Seller's own inventory (stock held by the seller).
/// Per PDF: "The Seller checks their own stock. If unavailable, they contact their assigned Distributor."
/// </summary>
public class SellerInventory
{
    public int Id { get; set; }
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public decimal UnitCost { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
