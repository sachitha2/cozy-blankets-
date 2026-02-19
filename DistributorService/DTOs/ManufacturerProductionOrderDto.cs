namespace DistributorService.DTOs;

/// <summary>
/// DTO for production order from ManufacturerService (for client deserialization)
/// </summary>
public class ManufacturerProductionOrderDto
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
