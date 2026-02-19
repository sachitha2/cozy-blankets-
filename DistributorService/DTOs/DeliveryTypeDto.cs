namespace DistributorService.DTOs;

/// <summary>
/// Data Transfer Object for DeliveryType information
/// </summary>
public class DeliveryTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public int EstimatedDays { get; set; }
    public bool IsActive { get; set; }
}
