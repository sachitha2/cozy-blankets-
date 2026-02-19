namespace ManufacturerService.DTOs;

/// <summary>
/// Data Transfer Object for Blanket information
/// </summary>
public class BlanketDto
{
    public int Id { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string Material { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> AdditionalImageUrls { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
