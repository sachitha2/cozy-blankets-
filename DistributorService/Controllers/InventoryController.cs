using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DistributorService.DTOs;
using DistributorService.Services;

namespace DistributorService.Controllers;

/// <summary>
/// API Controller for Inventory operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[EnableRateLimiting("fixed")]
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

    /// <summary>
    /// Create a new inventory item
    /// </summary>
    /// <param name="dto">Inventory creation details</param>
    /// <returns>Created inventory item</returns>
    /// <response code="201">Returns the created inventory item</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InventoryDto>> CreateInventory([FromBody] CreateInventoryDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (dto.BlanketId <= 0)
            {
                return BadRequest(new { error = "Invalid BlanketId" });
            }

            if (string.IsNullOrWhiteSpace(dto.ModelName))
            {
                return BadRequest(new { error = "ModelName is required" });
            }

            if (dto.Quantity < 0)
            {
                return BadRequest(new { error = "Quantity must be non-negative" });
            }

            var inventory = await _distributorService.AddInventoryAsync(dto);
            return CreatedAtAction(nameof(GetInventory), new { id = inventory.Id }, inventory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating inventory");
            return StatusCode(500, new { error = "An error occurred while creating inventory" });
        }
    }

    /// <summary>
    /// Update an existing inventory item
    /// </summary>
    /// <param name="id">Inventory item ID</param>
    /// <param name="dto">Inventory update details</param>
    /// <returns>Updated inventory item</returns>
    /// <response code="200">Returns the updated inventory item</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Inventory item not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(InventoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<InventoryDto>> UpdateInventory(int id, [FromBody] InventoryDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (id != dto.Id)
            {
                return BadRequest(new { error = "Id mismatch" });
            }

            if (dto.Quantity < 0)
            {
                return BadRequest(new { error = "Quantity must be non-negative" });
            }

            var inventory = await _distributorService.UpdateInventoryAsync(id, dto);
            return Ok(inventory);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Inventory item with Id {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating inventory with Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating inventory" });
        }
    }

    /// <summary>
    /// Delete an inventory item
    /// </summary>
    /// <param name="id">Inventory item ID</param>
    /// <returns>Success status</returns>
    /// <response code="200">Inventory item deleted successfully</response>
    /// <response code="400">Cannot delete inventory with reserved quantity</response>
    /// <response code="404">Inventory item not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteInventory(int id)
    {
        try
        {
            var result = await _distributorService.DeleteInventoryAsync(id);
            if (!result)
            {
                return NotFound(new { error = $"Inventory item with Id {id} not found" });
            }
            return Ok(new { message = "Inventory item deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting inventory with Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while deleting inventory" });
        }
    }
}
