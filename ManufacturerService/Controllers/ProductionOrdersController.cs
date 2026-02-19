using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ManufacturerService.DTOs;
using ManufacturerService.Services;

namespace ManufacturerService.Controllers;

/// <summary>
/// API Controller for committed production orders (backorder) and reverse fulfillment
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[EnableRateLimiting("fixed")]
[Produces("application/json")]
public class ProductionOrdersController : ControllerBase
{
    private readonly IProductionOrderService _productionOrderService;
    private readonly ILogger<ProductionOrdersController> _logger;

    public ProductionOrdersController(IProductionOrderService productionOrderService, ILogger<ProductionOrdersController> logger)
    {
        _productionOrderService = productionOrderService;
        _logger = logger;
    }

    /// <summary>
    /// Create a committed production order (backorder) for a distributor
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductionOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductionOrderResponseDto>> Create([FromBody] CreateProductionOrderRequestDto request)
    {
        try
        {
            if (request.Quantity <= 0)
            {
                return BadRequest(new { error = "Quantity must be positive" });
            }

            var result = await _productionOrderService.CreateAsync(request);
            if (result == null)
            {
                return BadRequest(new { error = "Cannot create production order (blanket not found or capacity not configured)" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating production order");
            return StatusCode(500, new { error = "An error occurred while creating the production order" });
        }
    }

    /// <summary>
    /// Get production order by distributor's external order id
    /// </summary>
    [HttpGet("by-external/{externalOrderId:int}")]
    [ProducesResponseType(typeof(ProductionOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductionOrderResponseDto>> GetByExternalOrderId(int externalOrderId)
    {
        try
        {
            var result = await _productionOrderService.GetByExternalOrderIdAsync(externalOrderId);
            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving production order by external id {ExternalOrderId}", externalOrderId);
            return StatusCode(500, new { error = "An error occurred while retrieving the production order" });
        }
    }

    /// <summary>
    /// Mark production as complete (adds quantity to manufacturer stock)
    /// </summary>
    [HttpPost("{id:int}/complete")]
    [ProducesResponseType(typeof(ProductionOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductionOrderResponseDto>> Complete(int id)
    {
        try
        {
            var result = await _productionOrderService.CompleteAsync(id);
            if (result == null)
            {
                return NotFound(new { error = "Production order not found or not in Pending status" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing production order {Id}", id);
            return StatusCode(500, new { error = "An error occurred while completing the production order" });
        }
    }

    /// <summary>
    /// Ship quantity to distributor (decrements manufacturer stock; call when distributor receives)
    /// </summary>
    [HttpPost("{id:int}/ship")]
    [ProducesResponseType(typeof(ProductionOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductionOrderResponseDto>> Ship(int id, [FromBody] ShipProductionRequestDto? request)
    {
        try
        {
            if (request == null || request.Quantity <= 0)
            {
                return BadRequest(new { error = "Quantity must be positive" });
            }

            var result = await _productionOrderService.ShipAsync(id, request.Quantity);
            if (result == null)
            {
                return NotFound(new { error = "Production order not found, not Completed, or insufficient stock" });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shipping production order {Id}", id);
            return StatusCode(500, new { error = "An error occurred while shipping the production order" });
        }
    }
}
