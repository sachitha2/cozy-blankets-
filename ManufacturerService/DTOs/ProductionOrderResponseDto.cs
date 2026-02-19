namespace ManufacturerService.DTOs;

/// <summary>
/// Response DTO for a production order
/// </summary>
public class ProductionOrderResponseDto
{
    public int Id { get; set; }
    public int BlanketId { get; set; }
    public string? ModelName { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ExternalOrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
}
