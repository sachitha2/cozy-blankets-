namespace ManufacturerService.DTOs;

/// <summary>
/// Data Transfer Object for ProductionCapacity information
/// </summary>
public class ProductionCapacityDto
{
    public int BlanketId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public int DailyCapacity { get; set; }
    public int LeadTimeDays { get; set; }
    public bool IsActive { get; set; }
    public DateTime LastUpdated { get; set; }
}
