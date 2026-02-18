namespace DistributorService.DTOs;

/// <summary>
/// Data Transfer Object for order request from seller
/// </summary>
public class OrderRequestDto
{
    public string SellerId { get; set; } = string.Empty;
    public int BlanketId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}
