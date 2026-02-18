namespace DistributorService.DTOs;

/// <summary>
/// Data Transfer Object for order response
/// </summary>
public class OrderResponseDto
{
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool FulfilledFromStock { get; set; }
    public bool RequiresManufacturerOrder { get; set; }
    public int? EstimatedDeliveryDays { get; set; }
}
