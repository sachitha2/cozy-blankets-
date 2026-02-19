namespace ManufacturerService.DTOs;

/// <summary>
/// Request DTO for shipping a completed production order to distributor
/// </summary>
public class ShipProductionRequestDto
{
    public int Quantity { get; set; }
}
