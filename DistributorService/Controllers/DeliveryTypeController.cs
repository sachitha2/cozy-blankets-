using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DistributorService.DTOs;
using DistributorService.Services;

namespace DistributorService.Controllers;

/// <summary>
/// API Controller for DeliveryType operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[EnableRateLimiting("fixed")]
[Produces("application/json")]
public class DeliveryTypeController : ControllerBase
{
    private readonly IDeliveryTypeService _deliveryTypeService;
    private readonly ILogger<DeliveryTypeController> _logger;

    public DeliveryTypeController(IDeliveryTypeService deliveryTypeService, ILogger<DeliveryTypeController> logger)
    {
        _deliveryTypeService = deliveryTypeService;
        _logger = logger;
    }

    /// <summary>
    /// Get all delivery types
    /// </summary>
    /// <returns>List of all delivery types</returns>
    /// <response code="200">Returns the list of delivery types</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DeliveryTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<DeliveryTypeDto>>> GetDeliveryTypes()
    {
        try
        {
            var deliveryTypes = await _deliveryTypeService.GetAllAsync();
            return Ok(deliveryTypes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery types");
            return StatusCode(500, new { error = "An error occurred while retrieving delivery types" });
        }
    }

    /// <summary>
    /// Get delivery type by ID
    /// </summary>
    /// <param name="id">Delivery type ID</param>
    /// <returns>Delivery type</returns>
    /// <response code="200">Returns the delivery type</response>
    /// <response code="404">Delivery type not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(DeliveryTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeliveryTypeDto>> GetDeliveryType(int id)
    {
        try
        {
            var deliveryType = await _deliveryTypeService.GetByIdAsync(id);
            if (deliveryType == null)
            {
                return NotFound(new { error = $"Delivery type with Id {id} not found" });
            }
            return Ok(deliveryType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery type with Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving delivery type" });
        }
    }

    /// <summary>
    /// Create a new delivery type
    /// </summary>
    /// <param name="dto">Delivery type creation details</param>
    /// <returns>Created delivery type</returns>
    /// <response code="201">Returns the created delivery type</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(DeliveryTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeliveryTypeDto>> CreateDeliveryType([FromBody] CreateDeliveryTypeDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { error = "Name is required" });
            }

            if (dto.EstimatedDays <= 0)
            {
                return BadRequest(new { error = "EstimatedDays must be greater than zero" });
            }

            var deliveryType = await _deliveryTypeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetDeliveryType), new { id = deliveryType.Id }, deliveryType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delivery type");
            return StatusCode(500, new { error = "An error occurred while creating delivery type" });
        }
    }

    /// <summary>
    /// Update an existing delivery type
    /// </summary>
    /// <param name="id">Delivery type ID</param>
    /// <param name="dto">Delivery type update details</param>
    /// <returns>Updated delivery type</returns>
    /// <response code="200">Returns the updated delivery type</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Delivery type not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DeliveryTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeliveryTypeDto>> UpdateDeliveryType(int id, [FromBody] UpdateDeliveryTypeDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { error = "Name is required" });
            }

            if (dto.EstimatedDays <= 0)
            {
                return BadRequest(new { error = "EstimatedDays must be greater than zero" });
            }

            var deliveryType = await _deliveryTypeService.UpdateAsync(id, dto);
            return Ok(deliveryType);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Delivery type with Id {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery type with Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating delivery type" });
        }
    }

    /// <summary>
    /// Delete a delivery type (soft delete)
    /// </summary>
    /// <param name="id">Delivery type ID</param>
    /// <returns>Success status</returns>
    /// <response code="200">Delivery type deleted successfully</response>
    /// <response code="404">Delivery type not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteDeliveryType(int id)
    {
        try
        {
            var result = await _deliveryTypeService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { error = $"Delivery type with Id {id} not found" });
            }
            return Ok(new { message = "Delivery type deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting delivery type with Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while deleting delivery type" });
        }
    }
}
