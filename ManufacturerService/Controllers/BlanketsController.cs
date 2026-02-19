using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ManufacturerService.DTOs;
using ManufacturerService.Services;

namespace ManufacturerService.Controllers;

/// <summary>
/// API Controller for Blanket operations
/// Implements RESTful API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[EnableRateLimiting("fixed")]
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
    /// Get a single blanket by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(BlanketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BlanketDto>> GetBlanketById(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid blanket ID" });
            var blanket = await _blanketService.GetBlanketByIdAsync(id);
            if (blanket == null)
                return NotFound(new { error = $"Blanket with ID {id} not found" });
            return Ok(blanket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blanket with Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the blanket" });
        }
    }

    /// <summary>
    /// Update blanket image URL (for storefront display)
    /// </summary>
    [HttpPatch("{id:int}/image")]
    [ProducesResponseType(typeof(BlanketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BlanketDto>> UpdateImage(int id, [FromBody] UpdateBlanketImageRequest request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid blanket ID" });
            var blanket = await _blanketService.UpdateImageUrlAsync(id, request?.ImageUrl);
            if (blanket == null)
                return NotFound(new { error = $"Blanket with ID {id} not found" });
            return Ok(blanket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating image for blanket Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating the image" });
        }
    }

    /// <summary>
    /// Add an additional image URL to the product gallery
    /// </summary>
    [HttpPost("{id:int}/images")]
    [ProducesResponseType(typeof(BlanketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BlanketDto>> AddAdditionalImage(int id, [FromBody] AddImageRequest request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid blanket ID" });
            if (request == null || string.IsNullOrWhiteSpace(request.ImageUrl))
                return BadRequest(new { error = "ImageUrl is required" });
            var blanket = await _blanketService.AddAdditionalImageUrlAsync(id, request.ImageUrl.Trim());
            if (blanket == null)
                return NotFound(new { error = $"Blanket with ID {id} not found" });
            return Ok(blanket);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding image for blanket Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while adding the image" });
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

    /// <summary>
    /// Create a new blanket model
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BlanketDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BlanketDto>> CreateBlanket([FromBody] CreateBlanketRequest request)
    {
        try
        {
            if (request == null)
                return BadRequest(new { error = "Request body is required" });

            if (string.IsNullOrWhiteSpace(request.ModelName))
                return BadRequest(new { error = "ModelName is required" });

            if (string.IsNullOrWhiteSpace(request.Material))
                return BadRequest(new { error = "Material is required" });

            if (request.UnitPrice < 0)
                return BadRequest(new { error = "UnitPrice cannot be negative" });

            var blanket = await _blanketService.CreateBlanketAsync(request);
            return CreatedAtAction(nameof(GetBlanketById), new { id = blanket.Id }, blanket);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot create blanket: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blanket");
            return StatusCode(500, new { error = "An error occurred while creating the blanket" });
        }
    }

    /// <summary>
    /// Update a blanket model (partial update)
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(BlanketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BlanketDto>> UpdateBlanket(int id, [FromBody] UpdateBlanketRequest request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid blanket ID" });

            if (request == null)
                return BadRequest(new { error = "Request body is required" });

            if (request.UnitPrice.HasValue && request.UnitPrice.Value < 0)
                return BadRequest(new { error = "UnitPrice cannot be negative" });

            var blanket = await _blanketService.UpdateBlanketAsync(id, request);
            if (blanket == null)
                return NotFound(new { error = $"Blanket with ID {id} not found" });

            return Ok(blanket);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot update blanket: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating blanket with Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating the blanket" });
        }
    }

    /// <summary>
    /// Get production capacity for a blanket
    /// </summary>
    [HttpGet("{id:int}/capacity")]
    [ProducesResponseType(typeof(ProductionCapacityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductionCapacityDto>> GetCapacity(int id)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid blanket ID" });

            var capacity = await _blanketService.GetCapacityByBlanketIdAsync(id);
            if (capacity == null)
                return NotFound(new { error = $"Production capacity not found for blanket ID {id}" });

            return Ok(capacity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving capacity for blanket Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving production capacity" });
        }
    }

    /// <summary>
    /// Update production capacity for a blanket
    /// </summary>
    [HttpPatch("{id:int}/capacity")]
    [ProducesResponseType(typeof(ProductionCapacityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductionCapacityDto>> UpdateCapacity(int id, [FromBody] UpdateProductionCapacityRequest request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid blanket ID" });

            if (request == null)
                return BadRequest(new { error = "Request body is required" });

            if (request.DailyCapacity.HasValue && request.DailyCapacity.Value <= 0)
                return BadRequest(new { error = "DailyCapacity must be greater than zero" });

            if (request.LeadTimeDays.HasValue && request.LeadTimeDays.Value < 0)
                return BadRequest(new { error = "LeadTimeDays cannot be negative" });

            var capacity = await _blanketService.UpdateCapacityAsync(id, request);
            if (capacity == null)
                return NotFound(new { error = $"Blanket with ID {id} not found" });

            return Ok(capacity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating capacity for blanket Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating production capacity" });
        }
    }

    /// <summary>
    /// Set stock quantity for a blanket
    /// </summary>
    [HttpPatch("{id:int}/stock")]
    [ProducesResponseType(typeof(StockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StockDto>> SetStock(int id, [FromBody] SetStockRequest request)
    {
        try
        {
            if (id <= 0)
                return BadRequest(new { error = "Invalid blanket ID" });

            if (request == null)
                return BadRequest(new { error = "Request body is required" });

            if (request.Quantity < 0)
                return BadRequest(new { error = "Quantity cannot be negative" });

            var stock = await _blanketService.SetStockAsync(id, request.Quantity);
            if (stock == null)
                return NotFound(new { error = $"Blanket with ID {id} not found" });

            return Ok(stock);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid stock request: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting stock for blanket Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while setting stock" });
        }
    }
}
