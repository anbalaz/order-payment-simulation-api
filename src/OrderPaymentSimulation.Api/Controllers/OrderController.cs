using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;
using order_payment_simulation_api.Services;
using System.Security.Claims;

namespace order_payment_simulation_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderPaymentDbContext _context;
    private readonly ILogger<OrderController> _logger;
    private readonly IKafkaProducerService _kafkaProducerService;

    public OrderController(
        OrderPaymentDbContext context,
        ILogger<OrderController> logger,
        IKafkaProducerService kafkaProducerService)
    {
        _context = context;
        _logger = logger;
        _kafkaProducerService = kafkaProducerService;
    }

    /// <summary>
    /// Get current authenticated user ID from JWT claims
    /// </summary>
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Get all orders for the current user
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<OrderDto>>> GetAll()
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == currentUserId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(orders.Select(OrderDto.CreateFrom).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orders");
            return StatusCode(500, new { message = "An error occurred while retrieving orders" });
        }
    }

    /// <summary>
    /// Get order by ID (with authorization check)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> Get(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            // Ensure user can only access their own orders
            if (order.UserId != currentUserId)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to access order {OrderId} owned by {OwnerId}",
                    currentUserId, id, order.UserId);
                return Forbid();
            }

            return Ok(OrderDto.CreateFrom(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the order" });
        }
    }

    /// <summary>
    /// Create a new order
    /// </summary>
    [HttpPut]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
            {
                _logger.LogError("Failed to extract user ID from JWT token");
                return Unauthorized(new { message = "Invalid authentication token" });
            }

            // Validate all products exist and have stock
            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // Check all products exist
            foreach (var item in request.Items)
            {
                if (!products.ContainsKey(item.ProductId))
                {
                    return UnprocessableEntity(new
                    {
                        message = $"Product with ID {item.ProductId} not found"
                    });
                }

                // Check stock availability
                var product = products[item.ProductId];
                if (product.Stock < item.Quantity)
                {
                    return UnprocessableEntity(new
                    {
                        message = $"Insufficient stock for product '{product.Name}'. Available: {product.Stock}, Requested: {item.Quantity}"
                    });
                }
            }

            // Calculate total from actual product prices (server-side for security)
            decimal total = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in request.Items)
            {
                var product = products[item.ProductId];
                var itemPrice = product.Price;
                total += itemPrice * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = itemPrice,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                // Decrease stock
                product.Stock -= item.Quantity;
            }

            var order = new Order
            {
                UserId = currentUserId,
                Total = total,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order created: {OrderId} for user {UserId}", order.Id, currentUserId);

            // Publish OrderCreated event to Kafka
            try
            {
                var orderCreatedEvent = new OrderCreatedEvent
                {
                    OrderId = order.Id,
                    UserId = currentUserId,
                    Total = total,
                    CreatedAt = order.CreatedAt
                };

                var topic = HttpContext.RequestServices
                    .GetRequiredService<IConfiguration>()
                    .GetSection("Kafka")["Topics:OrderCreated"];

                await _kafkaProducerService.PublishAsync(
                    topic!,
                    order.Id.ToString(),
                    orderCreatedEvent);

                _logger.LogInformation("OrderCreated event published for Order {OrderId}", order.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish OrderCreated event for Order {OrderId}", order.Id);
                // Don't fail the request if Kafka publish fails - order is already saved
            }

            // Reload with includes for response
            var createdOrder = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstAsync(o => o.Id == order.Id);

            return CreatedAtAction(
                nameof(Get),
                new { id = order.Id },
                OrderDto.CreateFrom(createdOrder));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order");
            return StatusCode(500, new { message = "An error occurred while creating the order" });
        }
    }

    /// <summary>
    /// Update order status
    /// </summary>
    [HttpPost("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderDto>> Update(int id, [FromBody] UpdateOrderRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != request.Id)
            {
                return BadRequest(new { message = "ID mismatch" });
            }

            var currentUserId = GetCurrentUserId();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            // Ensure user can only update their own orders
            if (order.UserId != currentUserId)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to update order {OrderId} owned by {OwnerId}",
                    currentUserId, id, order.UserId);
                return Forbid();
            }

            order.Status = request.Status;
            order.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order updated: {OrderId}, Status: {Status}", order.Id, order.Status);

            return Ok(OrderDto.CreateFrom(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the order" });
        }
    }

    /// <summary>
    /// Delete order (only pending orders can be deleted)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(new { message = "Order not found" });
            }

            // Ensure user can only delete their own orders
            if (order.UserId != currentUserId)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to delete order {OrderId} owned by {OwnerId}",
                    currentUserId, id, order.UserId);
                return Forbid();
            }

            // Business rule: Only pending orders can be deleted
            if (order.Status != OrderStatus.Pending)
            {
                return Conflict(new
                {
                    message = $"Cannot delete order with status '{order.Status}'. Only pending orders can be deleted.",
                    currentStatus = order.Status.ToString()
                });
            }

            // OrderItems will cascade delete (configured in OrderConfiguration)
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order deleted: {OrderId}", id);

            return Ok(new { message = "Order deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the order" });
        }
    }
}
