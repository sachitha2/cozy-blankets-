namespace DistributorService.DTOs;

/// <summary>
/// DTO for manufacturer production response
/// </summary>
public class ManufacturerProductionResponseDto
{
    public bool CanProduce { get; set; }
    public int AvailableStock { get; set; }
    public int LeadTimeDays { get; set; }
    public DateTime? EstimatedCompletionDate { get; set; }
    public string Message { get; set; } = string.Empty;
}
