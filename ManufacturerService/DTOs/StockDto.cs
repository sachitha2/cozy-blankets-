namespace ManufacturerService.DTOs;

/// <summary>
/// Data Transfer Object for Stock information
/// </summary>
public class StockDto
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public DateTime LastUpdated { get; set; }
}
