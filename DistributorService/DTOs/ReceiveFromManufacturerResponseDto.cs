namespace DistributorService.DTOs;

/// <summary>
/// Response when receiving stock from manufacturer (reverse fulfillment)
/// </summary>
public class ReceiveFromManufacturerResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? OrderId { get; set; }
    public string? OrderStatus { get; set; }
}
