using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SellerService.DTOs;
using SellerService.Services;

namespace SellerService.Controllers;

/// <summary>
/// API Controller for Seller Inventory operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[EnableRateLimiting("fixed")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly ISellerService _sellerService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(ISellerService sellerService, ILogger<InventoryController> logger)
    {
        _sellerService = sellerService;
        _logger = logger;
    }

    /// <summary>
    /// Get all seller inventory items
    /// </summary>
    /// <returns>List of all seller inventory items</returns>
    /// <response code="200">Returns the list of inventory items</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SellerInventoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<SellerInventoryDto>>> GetInventory()
    {
        try
        {
            var inventory = await _sellerService.GetSellerInventoryAsync();
            return Ok(inventory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving seller inventory");
            return StatusCode(500, new { error = "An error occurred while retrieving inventory" });
        }
    }
}
