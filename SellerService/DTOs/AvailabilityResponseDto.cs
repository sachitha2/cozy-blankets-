namespace SellerService.DTOs;

/// <summary>
/// Data Transfer Object for availability check response
/// </summary>
public class AvailabilityResponseDto
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public int AvailableQuantity { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? EstimatedDeliveryDays { get; set; }
}
