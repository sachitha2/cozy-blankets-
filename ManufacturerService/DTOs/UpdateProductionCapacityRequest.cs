namespace ManufacturerService.DTOs;

/// <summary>
/// Request DTO for updating production capacity (partial update)
/// </summary>
public class UpdateProductionCapacityRequest
{
    public int? DailyCapacity { get; set; }
    public int? LeadTimeDays { get; set; }
    public bool? IsActive { get; set; }
}
