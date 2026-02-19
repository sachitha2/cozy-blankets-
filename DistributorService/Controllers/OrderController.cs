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
    /// Get all orders
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <returns>List of orders</returns>
    /// <response code="200">Returns the list of orders</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrders([FromQuery] string? status = null)
    {
        try
        {
            var orders = await _distributorService.GetOrdersAsync(status);
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, new { error = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Get order by ID
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <returns>Order details</returns>
    /// <response code="200">Returns the order</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> GetOrder(int id)
    {
        try
        {
            var order = await _distributorService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound(new { error = $"Order with Id {id} not found" });
            }
            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order with Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving order" });
        }
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

    /// <summary>
    /// Update order status
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="dto">Status update details</param>
    /// <returns>Updated order</returns>
    /// <response code="200">Returns the updated order</response>
    /// <response code="400">Invalid request or invalid status transition</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id:int}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(dto.Status))
            {
                return BadRequest(new { error = "Status is required" });
            }

            var order = await _distributorService.UpdateOrderStatusAsync(id, dto.Status);
            return Ok(order);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = $"Order with Id {id} not found" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order status for Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating order status" });
        }
    }

    /// <summary>
    /// Update order delivery information
    /// </summary>
    /// <param name="id">Order ID</param>
    /// <param name="dto">Delivery update details</param>
    /// <returns>Updated order</returns>
    /// <response code="200">Returns the updated order</response>
    /// <response code="400">Invalid request</response>
    /// <response code="404">Order or delivery type not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("{id:int}/delivery")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> UpdateOrderDelivery(int id, [FromBody] UpdateOrderDeliveryDto dto)
    {
        try
        {
            if (dto == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            var order = await _distributorService.UpdateOrderDeliveryAsync(id, dto.DeliveryTypeId, dto.DeliveryAddress);
            return Ok(order);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order delivery for Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while updating order delivery" });
        }
    }
}
