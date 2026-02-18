namespace SellerService.DTOs;

/// <summary>
/// Data Transfer Object for customer order request
/// </summary>
public class CustomerOrderRequestDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public List<OrderItemRequestDto> Items { get; set; } = new();
}

public class OrderItemRequestDto
{
    public int BlanketId { get; set; }
    public int Quantity { get; set; }
}
