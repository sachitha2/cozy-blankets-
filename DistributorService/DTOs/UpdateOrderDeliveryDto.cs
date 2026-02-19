namespace DistributorService.DTOs;

/// <summary>
/// Data Transfer Object for updating order delivery information
/// </summary>
public class UpdateOrderDeliveryDto
{
    public int? DeliveryTypeId { get; set; }
    public string? DeliveryAddress { get; set; }
}
