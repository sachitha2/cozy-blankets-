namespace ManufacturerService.Models;

/// <summary>
/// Represents a committed production order (backorder) from a distributor.
/// </summary>
public class ProductionOrder
{
    public int Id { get; set; }
    public int BlanketId { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Completed, Shipped, Cancelled
    public int? ExternalOrderId { get; set; } // Distributor's Order.Id
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? ShippedAt { get; set; }

    public Blanket Blanket { get; set; } = null!;
}
