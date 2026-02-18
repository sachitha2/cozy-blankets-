namespace SellerService.Models;

/// <summary>
/// Represents an item in a customer order
/// </summary>
public class OrderItem
{
    public int Id { get; set; }
    public int CustomerOrderId { get; set; }
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal => Quantity * UnitPrice;
    public string Status { get; set; } = "Pending"; // Pending, Available, Unavailable, Fulfilled
    
    // Navigation property
    public CustomerOrder CustomerOrder { get; set; } = null!;
}
