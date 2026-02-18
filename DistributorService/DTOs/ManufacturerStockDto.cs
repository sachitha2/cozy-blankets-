namespace DistributorService.DTOs;

/// <summary>
/// DTO for manufacturer stock response
/// </summary>
public class ManufacturerStockDto
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public DateTime LastUpdated { get; set; }
}
