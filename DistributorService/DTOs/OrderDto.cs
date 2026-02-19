namespace DistributorService.DTOs;

/// <summary>
/// Data Transfer Object for Order information
/// </summary>
public class OrderDto
{
    public int Id { get; set; }
    public string SellerId { get; set; } = string.Empty;
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime? FulfilledDate { get; set; }
    public string? Notes { get; set; }
    public int? DeliveryTypeId { get; set; }
    public string? DeliveryTypeName { get; set; }
    public string? DeliveryAddress { get; set; }
}
