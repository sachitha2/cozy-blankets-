namespace DistributorService.DTOs;

/// <summary>
/// Data Transfer Object for creating inventory
/// </summary>
public class CreateInventoryDto
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}
