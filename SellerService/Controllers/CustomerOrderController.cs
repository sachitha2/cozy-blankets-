using Microsoft.AspNetCore.Mvc;
using SellerService.DTOs;
using SellerService.Services;

namespace SellerService.Controllers;

/// <summary>
/// API Controller for Customer Order operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CustomerOrderController : ControllerBase
{
    private readonly ISellerService _sellerService;
    private readonly ILogger<CustomerOrderController> _logger;

    public CustomerOrderController(ISellerService sellerService, ILogger<CustomerOrderController> logger)
    {
        _sellerService = sellerService;
        _logger = logger;
    }

    /// <summary>
    /// Process a customer order
    /// </summary>
    /// <param name="request">Customer order request details</param>
    /// <returns>Order response with fulfillment status</returns>
    /// <response code="200">Returns order response</response>
    /// <response code="400">Invalid request</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(CustomerOrderResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerOrderResponseDto>> ProcessCustomerOrder([FromBody] CustomerOrderRequestDto request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Request body is required" });
            }

            if (string.IsNullOrWhiteSpace(request.CustomerName))
            {
                return BadRequest(new { error = "CustomerName is required" });
            }

            if (string.IsNullOrWhiteSpace(request.CustomerEmail))
            {
                return BadRequest(new { error = "CustomerEmail is required" });
            }

            if (string.IsNullOrWhiteSpace(request.ShippingAddress))
            {
                return BadRequest(new { error = "ShippingAddress is required" });
            }

            if (request.Items == null || !request.Items.Any())
            {
                return BadRequest(new { error = "Order must contain at least one item" });
            }

            var response = await _sellerService.ProcessCustomerOrderAsync(request);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing customer order");
            return StatusCode(500, new { error = "An error occurred while processing the customer order" });
        }
    }

    /// <summary>
    /// Get all customer orders
    /// </summary>
    /// <returns>List of all customer orders</returns>
    /// <response code="200">Returns the list of customer orders</response>
    /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CustomerOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CustomerOrderDto>>> GetAllOrders()
    {
        try
        {
            var orders = await _sellerService.GetAllCustomerOrdersAsync();
            return Ok(orders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer orders");
            return StatusCode(500, new { error = "An error occurred while retrieving customer orders" });
        }
    }

    /// <summary>
    /// Get a specific customer order by ID
    /// </summary>
    /// <param name="id">The order ID</param>
    /// <returns>Customer order details</returns>
    /// <response code="200">Returns the customer order</response>
    /// <response code="404">Order not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerOrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CustomerOrderDto>> GetOrderById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Invalid order ID" });
            }

            var order = await _sellerService.GetCustomerOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound(new { error = $"Order with ID {id} not found" });
            }

            return Ok(order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customer order with Id: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the customer order" });
        }
    }
}
