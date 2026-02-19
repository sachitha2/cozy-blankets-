namespace DistributorService.DTOs;

/// <summary>
/// Data Transfer Object for updating a DeliveryType
/// </summary>
public class UpdateDeliveryTypeDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public int EstimatedDays { get; set; }
    public bool IsActive { get; set; }
}
