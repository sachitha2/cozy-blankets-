namespace ManufacturerService.DTOs;

/// <summary>
/// Request DTO for creating a committed production order (backorder)
/// </summary>
public class CreateProductionOrderRequestDto
{
    public int BlanketId { get; set; }
    public int Quantity { get; set; }
    /// <summary>
    /// Distributor's Order.Id (external reference for reverse fulfillment)
    /// </summary>
    public int? ExternalOrderId { get; set; }
}
