namespace SellerService.DTOs;

/// <summary>
/// Data Transfer Object for customer order response
/// </summary>
public class CustomerOrderResponseDto
{
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<OrderItemStatusDto> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class OrderItemStatusDto
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
