namespace ManufacturerService.DTOs;

/// <summary>
/// Data Transfer Object for production response
/// </summary>
public class ProductionResponseDto
{
    public bool CanProduce { get; set; }
    public int AvailableStock { get; set; }
    public int LeadTimeDays { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public string Message { get; set; } = string.Empty;
}
