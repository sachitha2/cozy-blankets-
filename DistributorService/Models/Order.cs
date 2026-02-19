namespace DistributorService.Models;

/// <summary>
/// Represents an order received from a seller
/// </summary>
public class Order
{
    public int Id { get; set; }
    public string SellerId { get; set; } = string.Empty;
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Fulfilled, Cancelled, PendingManufacturer
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledDate { get; set; }
    public string? Notes { get; set; }
    public int? DeliveryTypeId { get; set; } // Foreign key to DeliveryType
    public string? DeliveryAddress { get; set; } // Delivery address for the order
}
