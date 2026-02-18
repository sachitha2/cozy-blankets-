using Microsoft.AspNetCore.Mvc;
using ManufacturerService.DTOs;
using ManufacturerService.Services;

namespace ManufacturerService.Controllers;

/// <summary>
/// API Controller for Blanket operations
/// Implements RESTful API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class BlanketsController : ControllerBase
{
    private readonly IBlanketService _blanketService;
    private readonly ILogger<BlanketsController> _logger;

    public BlanketsController(IBlanketService blanketService, ILogger<BlanketsController> logger)
    {
        _blanketService = blanketService;
        _logger = logger;
    }

    /// <summary>
    /// Get all blanket models
    /// </summary>
    /// <returns>List of all blankets</returns>
    /// <response code="200">Returns the list of blankets</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BlanketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<BlanketDto>>> GetBlankets()
    {
        try
        {
            var blankets = await _blanketService.GetAllBlanketsAsync();
            return Ok(blankets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blankets");
            return StatusCode(500, new { error = "An error occurred while retrieving blankets" });
        }
    }

    /// <summary>
    /// Get stock information for a specific blanket model
    /// </summary>
    /// <param name="modelId">The ID of the blanket model</param>
    /// <returns>Stock information</returns>
    /// <response code="200">Returns the stock information</response>
    /// <response code="404">Blanket model not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("stock/{modelId}")]
    [ProducesResponseType(typeof(StockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StockDto>> GetStock(int modelId)
    {
        try
        {
            if (modelId <= 0)
            {
                return BadRequest(new { error = "Invalid model ID" });
            }

            var stock = await _blanketService.GetStockByModelIdAsync(modelId);
            if (stock == null)
            {
                return NotFound(new { error = $"Stock information not found for model ID {modelId}" });
            }

            return Ok(stock);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving stock for modelId: {ModelId}", modelId);
            return StatusCode(500, new { error = "An error occurred while retrieving stock information" });
        }
    }

    /// <summary>
    /// Process a production request
    /// </summary>
    /// <param name="request">Production request details</param>
    /// <returns>Production response with availability and lead time</returns>
    /// <response code="200">Returns production response</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("produce")]
    [ProducesResponseType(typeof(ProductionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductionResponseDto>> Produce([FromBody] ProductionRequestDto request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (request.BlanketId <= 0)
            {
                return BadRequest(new { error = "Invalid blanket ID" });
            }

            if (request.Quantity <= 0)
            {
                return BadRequest(new { error = "Quantity must be greater than zero" });
            }

            var response = await _blanketService.ProcessProductionRequestAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing production request");
            return StatusCode(500, new { error = "An error occurred while processing the production request" });
        }
    }
}
