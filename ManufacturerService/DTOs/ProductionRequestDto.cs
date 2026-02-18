namespace ManufacturerService.DTOs;

/// <summary>
/// Data Transfer Object for production request
/// </summary>
public class ProductionRequestDto
{
    public int BlanketId { get; set; }
    public int Quantity { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
}
