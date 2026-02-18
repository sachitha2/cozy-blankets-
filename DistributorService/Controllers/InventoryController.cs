using Microsoft.AspNetCore.Mvc;
using DistributorService.DTOs;
using DistributorService.Services;

namespace DistributorService.Controllers;

/// <summary>
/// API Controller for Inventory operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class InventoryController : ControllerBase
{
    private readonly IDistributorService _distributorService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IDistributorService distributorService, ILogger<InventoryController> logger)
    {
        _distributorService = distributorService;
        _logger = logger;
    }

    /// <summary>
    /// Get all inventory items
    /// </summary>
    /// <returns>List of all inventory items</returns>
    /// <response code="200">Returns the list of inventory items</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<InventoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> GetInventory()
    {
        try
        {
            var inventory = await _distributorService.GetInventoryAsync();
            return Ok(inventory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving inventory");
            return StatusCode(500, new { error = "An error occurred while retrieving inventory" });
        }
    }
}
