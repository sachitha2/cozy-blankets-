using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using DistributorService.DTOs;
using DistributorService.Services;

namespace DistributorService.Controllers;

/// <summary>
/// API Controller for Order operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[EnableRateLimiting("fixed")]
[Produces("application/json")]
public class OrderController : ControllerBase
{
    private readonly IDistributorService _distributorService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(IDistributorService distributorService, ILogger<OrderController> logger)
    {
        _distributorService = distributorService;
        _logger = logger;
    }

    /// <summary>
    /// Process an order from a seller
    /// </summary>
    /// <param name="request">Order request details</param>
    /// <returns>Order response with fulfillment status</returns>
    /// <response code="200">Returns order response</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderResponseDto>> ProcessOrder([FromBody] OrderRequestDto request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(request.SellerId))
            {
                return BadRequest(new { error = "SellerId is required" });
            }

            if (request.BlanketId <= 0)
            {
                return BadRequest(new { error = "Invalid blanket ID" });
            }

            if (request.Quantity <= 0)
            {
                return BadRequest(new { error = "Quantity must be greater than zero" });
            }

            var response = await _distributorService.ProcessOrderAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order");
            return StatusCode(500, new { error = "An error occurred while processing the order" });
        }
    }

    /// <summary>
    /// Receive stock from manufacturer and fulfill a pending order (reverse fulfillment)
    /// </summary>
    /// <param name="id">Distributor order id (must be in PendingManufacturer status)</param>
    /// <response code="200">Returns receive result</response>
    /// <response code="400">Order not found or not pending manufacturer</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("{id:int}/receive-from-manufacturer")]
    [ProducesResponseType(typeof(ReceiveFromManufacturerResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ReceiveFromManufacturerResponseDto>> ReceiveFromManufacturer(int id)
    {
        try
        {
            var response = await _distributorService.ReceiveFromManufacturerAsync(id);
            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error receiving from manufacturer for order {OrderId}", id);
            return StatusCode(500, new { error = "An error occurred while receiving from manufacturer" });
        }
    }
}
