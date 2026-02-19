namespace ManufacturerService.DTOs;

/// <summary>
/// Request DTO for updating a blanket (partial update)
/// </summary>
public class UpdateBlanketRequest
{
    public string? ModelName { get; set; }
    public string? Material { get; set; }
    public string? Description { get; set; }
    public decimal? UnitPrice { get; set; }
}
