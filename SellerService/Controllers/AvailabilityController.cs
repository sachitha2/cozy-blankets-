using Microsoft.AspNetCore.Mvc;
using SellerService.DTOs;
using SellerService.Services;

namespace SellerService.Controllers;

/// <summary>
/// API Controller for Availability operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AvailabilityController : ControllerBase
{
    private readonly ISellerService _sellerService;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(ISellerService sellerService, ILogger<AvailabilityController> logger)
    {
        _sellerService = sellerService;
        _logger = logger;
    }

    /// <summary>
    /// Check availability of a blanket model
    /// </summary>
    /// <param name="modelId">The ID of the blanket model</param>
    /// <returns>Availability information</returns>
    /// <response code="200">Returns availability information</response>
    /// <response code="400">Invalid model ID</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{modelId}")]
    [ProducesResponseType(typeof(AvailabilityResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AvailabilityResponseDto>> CheckAvailability(int modelId)
    {
        try
        {
            if (modelId <= 0)
            {
                return BadRequest(new { error = "Invalid model ID" });
            }

            var availability = await _sellerService.CheckAvailabilityAsync(modelId);
            return Ok(availability);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking availability for modelId: {ModelId}", modelId);
            return StatusCode(500, new { error = "An error occurred while checking availability" });
        }
    }
}
