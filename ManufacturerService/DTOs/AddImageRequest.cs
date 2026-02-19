namespace ManufacturerService.DTOs;

/// <summary>
/// Request to add an image URL to a blanket's gallery
/// </summary>
public class AddImageRequest
{
    public string ImageUrl { get; set; } = string.Empty;
}
