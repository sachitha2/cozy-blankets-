namespace ManufacturerService.DTOs;

/// <summary>
/// Request body for updating a blanket's image URL
/// </summary>
public class UpdateBlanketImageRequest
{
    public string? ImageUrl { get; set; }
}
