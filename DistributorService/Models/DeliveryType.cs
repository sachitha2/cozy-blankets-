namespace DistributorService.Models;

/// <summary>
/// Represents a delivery type/logistics option for orders
/// </summary>
public class DeliveryType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Standard", "Express", "Overnight"
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public int EstimatedDays { get; set; }
    public bool IsActive { get; set; } = true;
}
