namespace DistributorService.DTOs;

/// <summary>
/// Data Transfer Object for creating a DeliveryType
/// </summary>
public class CreateDeliveryTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public int EstimatedDays { get; set; }
}
