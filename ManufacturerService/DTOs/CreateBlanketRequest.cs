namespace ManufacturerService.DTOs;

/// <summary>
/// Request DTO for creating a new blanket
/// </summary>
public class CreateBlanketRequest
{
    public string ModelName { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? ImageUrl { get; set; }
}
