namespace SellerService.Models;

/// <summary>
/// Represents a customer order received by the seller
/// </summary>
public class CustomerOrder
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending, Processing, Fulfilled, Cancelled
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? FulfilledDate { get; set; }
    public decimal TotalAmount { get; set; }
    
    // Navigation property
    public List<OrderItem> OrderItems { get; set; } = new();
}
