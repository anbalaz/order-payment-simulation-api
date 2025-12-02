# PRP: Product and Order CRUD REST APIs

## Executive Summary

Implement complete CRUD REST APIs for Products and Orders in an ASP.NET Core 8.0 application with PostgreSQL database, following existing patterns from the User module. This includes entity model updates, DTOs, controllers with JWT authentication, comprehensive tests, and documentation.

**Confidence Score**: 9/10 - Well-defined requirements with clear existing patterns to follow.

---

## 🎯 Critical Context

### Existing Codebase Patterns

This project already has a fully implemented User CRUD module that serves as the blueprint:

**Key Files to Reference**:
- **Controller Pattern**: `src/OrderPaymentSimulation.Api/Controllers/UserController.cs` (lines 1-235)
- **Model Pattern**: `src/OrderPaymentSimulation.Api/Models/User.cs`
- **DTO Pattern**: `src/OrderPaymentSimulation.Api/Dtos/UserDto.cs` (lines 16-24)
- **Request DTOs**: `CreateUserRequest.cs` (with Data Annotations) and `UpdateUserRequest.cs`
- **Configuration Pattern**: `src/OrderPaymentSimulation.Api/Data/Configurations/UserConfiguration.cs`
- **Integration Test Pattern**: `test/IntegrationTests/IntegrationTests/UserControllerTests.cs` (lines 1-170)
- **Unit Test Pattern**: `test/UnitTests/UnitTests/DtoMappingTests.cs`

### Current Database Schema

**PostgreSQL Connection** (already configured):
```
Host: localhost:5432
Database: order_payment_simulation
User: orderuser
Container: order-payment-db
```

**Existing Tables** (via EnsureCreated):
- `users` - Fully implemented
- `products` - Model exists but MISSING `stock` column
- `orders` - Model exists, fully configured
- `order_items` - Model exists but MISSING `updated_at` column

### ⚠️ Critical Issues to Address

1. **Product.Stock Field Missing**
   - Current: `src/OrderPaymentSimulation.Api/Models/Product.cs` has Id, Name, Description, Price, CreatedAt
   - Required: Add `Stock` property (int >= 0)
   - Impact: Must update ProductConfiguration.cs to add column mapping

2. **OrderItem.UpdatedAt Field Missing**
   - Current: `src/OrderPaymentSimulation.Api/Models/OrderItem.cs` has Id, OrderId, ProductId, Quantity, Price, CreatedAt
   - Required: Add `UpdatedAt` property (DateTime)
   - Impact: Must update OrderItemConfiguration.cs to add column mapping

3. **OrderStatus Enum Discrepancy**
   - Current: `src/OrderPaymentSimulation.Api/Models/OrderStatus.cs` has values: Pending(0), Processing(1), Completed(2), Cancelled(3)
   - Requirements: `pending`, `processing`, `completed`, `expired`
   - **Decision**: Add `Expired = 4` as 5th status (maintains backward compatibility)
   - Alternative: Replace `Cancelled` with `Expired` (breaking change to seed data)
   - **Recommendation**: Add as 5th status to avoid breaking existing seed data

4. **Database Recreation Required**
   - After model changes, the database schema must be recreated
   - Current approach uses `EnsureCreated()` (not migrations)
   - Action: Drop and recreate database after model updates

---

## 📚 External Documentation

### ASP.NET Core 8.0 Resources
- **Data Annotations**: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
- **Controller Actions**: https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types
- **JWT Authentication**: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/

### Entity Framework Core 8.0
- **Fluent API Configuration**: https://learn.microsoft.com/en-us/ef/core/modeling/
- **Data Annotations**: https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties
- **Relationships**: https://learn.microsoft.com/en-us/ef/core/modeling/relationships

### Testing Libraries (Already Installed)
- **xUnit**: https://xunit.net/docs/getting-started/netcore/cmdline
- **FluentAssertions**: https://fluentassertions.com/introduction
- **AutoFixture**: https://www.nuget.org/packages/autofixture (for future use)
- **Moq**: https://www.nuget.org/packages/moq/ (for future use)

---

## 🏗️ Implementation Blueprint

### Phase 1: Model Updates (Foundation)

#### 1.1 Update Product Model
**File**: `src/OrderPaymentSimulation.Api/Models/Product.cs`

**Current State** (lines 1-13):
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

**Required Change**:
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }  // NEW FIELD
    public DateTime CreatedAt { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

#### 1.2 Update OrderItem Model
**File**: `src/OrderPaymentSimulation.Api/Models/OrderItem.cs`

**Required Change**: Add `UpdatedAt` property after `CreatedAt`:
```csharp
public DateTime UpdatedAt { get; set; }  // NEW FIELD
```

#### 1.3 Update OrderStatus Enum
**File**: `src/OrderPaymentSimulation.Api/Models/OrderStatus.cs`

**Current State**:
```csharp
public enum OrderStatus : short
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Cancelled = 3
}
```

**Required Change** (Add 5th status):
```csharp
public enum OrderStatus : short
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Cancelled = 3,
    Expired = 4  // NEW STATUS
}
```

#### 1.4 Update ProductConfiguration
**File**: `src/OrderPaymentSimulation.Api/Data/Configurations/ProductConfiguration.cs`

**Pattern Reference**: `UserConfiguration.cs` lines 16-40 (property configuration with snake_case)

**Required Addition** (after Price configuration, before CreatedAt):
```csharp
builder.Property(p => p.Stock)
    .HasColumnName("stock")
    .IsRequired()
    .HasDefaultValue(0);  // Default stock to 0 for new products
```

**Validation Consideration**: EF Core doesn't enforce >= 0 at database level for integers. This will be handled by DTO validation.

#### 1.5 Update OrderItemConfiguration
**File**: `src/OrderPaymentSimulation.Api/Data/Configurations/OrderItemConfiguration.cs`

**Pattern Reference**: `OrderConfiguration.cs` lines 37-39 (UpdatedAt with default)

**Required Addition** (after CreatedAt configuration):
```csharp
builder.Property(oi => oi.UpdatedAt)
    .HasColumnName("updated_at")
    .HasDefaultValueSql("CURRENT_TIMESTAMP");
```

#### 1.6 Update SeedData
**File**: `src/OrderPaymentSimulation.Api/Data/SeedData.cs`

**Required Changes**:
1. Add `Stock` values to product seeds (lines 47-54)
2. Add `UpdatedAt` values to order item seeds (lines 92-104)

**Example**:
```csharp
new Product {
    Name = "Laptop",
    Description = "High-performance laptop",
    Price = 999.99m,
    Stock = 50,  // NEW
    CreatedAt = DateTime.UtcNow
}
```

```csharp
new OrderItem {
    OrderId = order1.Id,
    ProductId = products[0].Id,
    Quantity = 1,
    Price = 999.99m,
    CreatedAt = DateTime.UtcNow.AddDays(-5),
    UpdatedAt = DateTime.UtcNow.AddDays(-5)  // NEW
}
```

---

### Phase 2: Product DTOs

**Directory**: `src/OrderPaymentSimulation.Api/Dtos/`

#### 2.1 ProductDto.cs (Response DTO)
**Pattern Reference**: `UserDto.cs` lines 1-25

```csharp
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Dtos;

/// <summary>
/// DTO for Product response
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public DateTime CreatedAt { get; set; }

    public static ProductDto CreateFrom(Product product)
        => new()
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Stock = product.Stock,
            CreatedAt = product.CreatedAt
        };
}
```

#### 2.2 CreateProductRequest.cs (Create DTO)
**Pattern Reference**: `CreateUserRequest.cs` lines 1-19

```csharp
using System.ComponentModel.DataAnnotations;

namespace order_payment_simulation_api.Dtos;

public class CreateProductRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Stock is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock must be greater than or equal to 0")]
    public int Stock { get; set; }
}
```

#### 2.3 UpdateProductRequest.cs (Update DTO)
**Pattern Reference**: `UpdateUserRequest.cs` lines 1-24

```csharp
using System.ComponentModel.DataAnnotations;

namespace order_payment_simulation_api.Dtos;

public class UpdateProductRequest
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Stock is required")]
    [Range(0, int.MaxValue, ErrorMessage = "Stock must be greater than or equal to 0")]
    public int Stock { get; set; }
}
```

---

### Phase 3: Order DTOs

#### 3.1 OrderItemDto.cs (Nested Response DTO)

```csharp
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Dtos;

/// <summary>
/// DTO for OrderItem response (includes product details)
/// </summary>
public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;  // Denormalized for convenience
    public int Quantity { get; set; }
    public decimal Price { get; set; }  // Price at time of order
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static OrderItemDto CreateFrom(OrderItem item)
        => new()
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.Product?.Name ?? string.Empty,
            Quantity = item.Quantity,
            Price = item.Price,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
}
```

#### 3.2 OrderDto.cs (Response DTO)

```csharp
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Dtos;

/// <summary>
/// DTO for Order response (includes nested order items)
/// </summary>
public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();

    public static OrderDto CreateFrom(Order order)
        => new()
        {
            Id = order.Id,
            UserId = order.UserId,
            Total = order.Total,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.OrderItems?.Select(OrderItemDto.CreateFrom).ToList() ?? new()
        };
}
```

#### 3.3 CreateOrderItemRequest.cs (Nested Create DTO)

```csharp
using System.ComponentModel.DataAnnotations;

namespace order_payment_simulation_api.Dtos;

public class CreateOrderItemRequest
{
    [Required(ErrorMessage = "ProductId is required")]
    [Range(1, int.MaxValue, ErrorMessage = "ProductId must be a valid ID")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }
}
```

#### 3.4 CreateOrderRequest.cs (Create DTO)

```csharp
using System.ComponentModel.DataAnnotations;

namespace order_payment_simulation_api.Dtos;

public class CreateOrderRequest
{
    [Required(ErrorMessage = "Items are required")]
    [MinLength(1, ErrorMessage = "Order must have at least one item")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}
```

**Note**: `Total` and `UserId` are NOT in the request DTO because:
- `UserId` comes from JWT claims (security requirement)
- `Total` is calculated server-side from product prices (data integrity)

#### 3.5 UpdateOrderRequest.cs (Update DTO)

```csharp
using System.ComponentModel.DataAnnotations;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Dtos;

public class UpdateOrderRequest
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Status is required")]
    public OrderStatus Status { get; set; }
}
```

**Note**: Only status updates are allowed. Items cannot be modified after order creation (business rule).

---

### Phase 4: ProductController

**File**: `src/OrderPaymentSimulation.Api/Controllers/ProductController.cs`

**Pattern Reference**: `UserController.cs` (lines 1-235) - Follow this structure exactly

#### Controller Structure Pseudocode

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly OrderPaymentDbContext _context;
    private readonly ILogger<ProductController> _logger;

    public ProductController(OrderPaymentDbContext context, ILogger<ProductController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // 1. GET /api/product - List all products
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<ProductDto>>> GetAll()
    {
        try
        {
            var products = await _context.Products.ToListAsync();
            return Ok(products.Select(ProductDto.CreateFrom).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, new { message = "An error occurred while retrieving products" });
        }
    }

    // 2. GET /api/product/{id} - Get product by ID
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductDto>> Get(int id)
    {
        // Pattern: UserController.cs lines 172-190
        try
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            return Ok(ProductDto.CreateFrom(product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product {ProductId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the product" });
        }
    }

    // 3. POST /api/product - Create new product
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductRequest request)
    {
        // Pattern: UserController.cs lines 46-90
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = new Product
            {
                Name = request.Name,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product created: {ProductId}", product.Id);

            return CreatedAtAction(
                nameof(Get),
                new { id = product.Id },
                ProductDto.CreateFrom(product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, new { message = "An error occurred while creating the product" });
        }
    }

    // 4. PUT /api/product/{id} - Update product
    [HttpPut("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ProductDto>> Update(int id, [FromBody] UpdateProductRequest request)
    {
        // Pattern: UserController.cs lines 102-161
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (id != request.Id)
                return BadRequest(new { message = "ID mismatch" });

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { message = "Product not found" });

            product.Name = request.Name;
            product.Description = request.Description;
            product.Price = request.Price;
            product.Stock = request.Stock;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Product updated: {ProductId}", product.Id);

            return Ok(ProductDto.CreateFrom(product));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the product" });
        }
    }

    // 5. DELETE /api/product/{id} - Delete product
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id)
    {
        // Pattern: UserController.cs lines 201-234
        try
        {
            var product = await _context.Products
                .Include(p => p.OrderItems)  // Check for references
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound(new { message = "Product not found" });

            // Check if product is referenced in any orders
            if (product.OrderItems.Any())
            {
                return Conflict(new {
                    message = "Cannot delete product that is referenced in orders",
                    orderCount = product.OrderItems.Select(oi => oi.OrderId).Distinct().Count()
                });
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product deleted: {ProductId}", id);

            return Ok(new { message = "Product deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product {ProductId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the product" });
        }
    }
}
```

**Key Differences from UserController**:
- Added `GetAll()` endpoint for listing products
- DELETE includes check for OrderItems references (returns 409 Conflict if referenced)
- No password hashing logic
- No authorization checks limiting to current user (all authenticated users can manage products)

**Note**: ProductConfiguration has `DeleteBehavior.Restrict` on OrderItems relationship. The explicit check in DELETE provides better error message than relying on database constraint violation.

---

### Phase 5: OrderController

**File**: `src/OrderPaymentSimulation.Api/Controllers/OrderController.cs`

#### Critical Authorization Logic

**Pattern Reference**: `UserController.cs` lines 32-36 (GetCurrentUserId helper)

```csharp
private int GetCurrentUserId()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return int.TryParse(userIdClaim, out var userId) ? userId : 0;
}
```

**Security Rule**: Users can ONLY access their own orders. All endpoints must filter by UserId from JWT claims.

#### Controller Structure Pseudocode

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderPaymentDbContext _context;
    private readonly ILogger<OrderController> _logger;

    public OrderController(OrderPaymentDbContext context, ILogger<OrderController> logger)
    {
        _context = context;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    // 1. GET /api/order - List user's orders
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
                .Where(o => o.UserId == currentUserId)  // CRITICAL: Filter by current user
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

    // 2. GET /api/order/{id} - Get order by ID (with authorization check)
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
                return NotFound(new { message = "Order not found" });

            // CRITICAL: Ensure user can only access their own orders
            if (order.UserId != currentUserId)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to access order {OrderId} owned by {OwnerId}",
                    currentUserId, id, order.UserId);
                return Forbid();  // 403 Forbidden
            }

            return Ok(OrderDto.CreateFrom(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving order {OrderId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the order" });
        }
    }

    // 3. POST /api/order - Create new order
    [HttpPost]
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
                return BadRequest(ModelState);

            var currentUserId = GetCurrentUserId();

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
                    return UnprocessableEntity(new {
                        message = $"Product with ID {item.ProductId} not found"
                    });
                }

                // Check stock availability
                var product = products[item.ProductId];
                if (product.Stock < item.Quantity)
                {
                    return UnprocessableEntity(new {
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
                var itemPrice = product.Price;  // Use current price
                total += itemPrice * item.Quantity;

                orderItems.Add(new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = itemPrice,  // Store price at time of order
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                // Decrease stock
                product.Stock -= item.Quantity;
            }

            var order = new Order
            {
                UserId = currentUserId,  // From JWT claims
                Total = total,           // Calculated server-side
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderItems = orderItems
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order created: {OrderId} for user {UserId}", order.Id, currentUserId);

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

    // 4. PUT /api/order/{id} - Update order status
    [HttpPut("{id}")]
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
                return BadRequest(ModelState);

            if (id != request.Id)
                return BadRequest(new { message = "ID mismatch" });

            var currentUserId = GetCurrentUserId();

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound(new { message = "Order not found" });

            // CRITICAL: Ensure user can only update their own orders
            if (order.UserId != currentUserId)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to update order {OrderId} owned by {OwnerId}",
                    currentUserId, id, order.UserId);
                return Forbid();  // 403 Forbidden
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

    // 5. DELETE /api/order/{id} - Delete order (cancel)
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
                return NotFound(new { message = "Order not found" });

            // CRITICAL: Ensure user can only delete their own orders
            if (order.UserId != currentUserId)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to delete order {OrderId} owned by {OwnerId}",
                    currentUserId, id, order.UserId);
                return Forbid();  // 403 Forbidden
            }

            // Business rule: Only pending orders can be deleted
            if (order.Status != OrderStatus.Pending)
            {
                return Conflict(new {
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
```

**Key Features**:
- **Stock Management**: Create endpoint decreases product stock automatically
- **Price Locking**: Order items store price at time of order (not current price)
- **Authorization**: All endpoints verify order ownership via JWT claims
- **Business Rules**:
  - Only pending orders can be deleted
  - Total is calculated server-side (not trusted from client)
  - Stock validation before order creation

---

### Phase 6: Integration Tests

**Directory**: `test/IntegrationTests/IntegrationTests/`

#### 6.1 ProductControllerTests.cs

**Pattern Reference**: `UserControllerTests.cs` (lines 1-170)

**Required Tests** (minimum 7):

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;

namespace IntegrationTests;

public class ProductControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProductControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateProduct_WithValidData_ReturnsCreated()
    {
        // Arrange
        var (_, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Stock = 100
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/product", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var productDto = await response.Content.ReadFromJsonAsync<ProductDto>();
        productDto.Should().NotBeNull();
        productDto!.Name.Should().Be("Test Product");
        productDto.Stock.Should().Be(100);
    }

    [Fact]
    public async Task CreateProduct_WithNegativePrice_ReturnsBadRequest()
    {
        // Test Data Annotations validation
        // Price with -1 should fail [Range(0, double.MaxValue)]
    }

    [Fact]
    public async Task CreateProduct_WithNegativeStock_ReturnsBadRequest()
    {
        // Test Data Annotations validation
        // Stock with -1 should fail [Range(0, int.MaxValue)]
    }

    [Fact]
    public async Task GetProduct_WithAuthentication_ReturnsProduct()
    {
        // Pattern: UserControllerTests.cs lines 76-90
    }

    [Fact]
    public async Task GetProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Pattern: UserControllerTests.cs lines 93-100
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_ReturnsOk()
    {
        // Pattern: UserControllerTests.cs lines 103-123
    }

    [Fact]
    public async Task GetAllProducts_WithAuthentication_ReturnsProducts()
    {
        // Unique to ProductController
    }

    [Fact]
    public async Task DeleteProduct_NotReferencedInOrders_ReturnsOk()
    {
        // Test successful deletion
    }

    [Fact]
    public async Task DeleteProduct_ReferencedInOrders_ReturnsConflict()
    {
        // Create order with product, then try to delete product
        // Should return 409 Conflict
    }

    private async Task<(int userId, string token)> CreateAndLoginUser()
    {
        // Pattern: UserControllerTests.cs lines 145-169
        var email = $"user{Guid.NewGuid()}@example.com";

        var createRequest = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = "Password123!"
        };
        var createResponse = await _client.PutAsJsonAsync("/api/user", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var loginRequest = new LoginRequest
        {
            Email = email,
            Password = "Password123!"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (createdUser!.Id, loginResult!.Token);
    }
}
```

#### 6.2 OrderControllerTests.cs

**Required Tests** (minimum 8):

```csharp
public class OrderControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    // Similar structure to ProductControllerTests

    [Fact]
    public async Task CreateOrder_WithValidItems_ReturnsCreated()
    {
        // 1. Create user and login
        // 2. Create products with stock
        // 3. Create order with items
        // 4. Assert 201 Created
        // 5. Verify order total is calculated correctly
        // 6. Verify stock decreased
    }

    [Fact]
    public async Task CreateOrder_WithInvalidProductId_ReturnsUnprocessableEntity()
    {
        // Test 422 when product doesn't exist
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_ReturnsUnprocessableEntity()
    {
        // Create product with stock = 5
        // Try to order quantity = 10
        // Should return 422
    }

    [Fact]
    public async Task CreateOrder_WithQuantityZero_ReturnsBadRequest()
    {
        // Test Data Annotations validation
        // Quantity with 0 should fail [Range(1, int.MaxValue)]
    }

    [Fact]
    public async Task GetOrder_OwnOrder_ReturnsOk()
    {
        // User gets their own order - 200 OK
    }

    [Fact]
    public async Task GetOrder_OtherUsersOrder_ReturnsForbidden()
    {
        // User A creates order
        // User B tries to get User A's order
        // Should return 403 Forbidden
    }

    [Fact]
    public async Task UpdateOrderStatus_OwnOrder_ReturnsOk()
    {
        // Create order, update status, verify updated
    }

    [Fact]
    public async Task DeleteOrder_PendingOrder_ReturnsOk()
    {
        // Create pending order, delete it, verify deleted
    }

    [Fact]
    public async Task DeleteOrder_ProcessingOrder_ReturnsConflict()
    {
        // Create order, update status to Processing
        // Try to delete - should return 409 Conflict
    }

    [Fact]
    public async Task GetAllOrders_ReturnsOnlyCurrentUsersOrders()
    {
        // User A creates 2 orders
        // User B creates 1 order
        // User A calls GetAll - should only see their 2 orders
    }

    private async Task<int> CreateProductWithStock(string token, int stock = 100)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = new CreateProductRequest
        {
            Name = $"Product {Guid.NewGuid()}",
            Description = "Test",
            Price = 10.00m,
            Stock = stock
        };

        var response = await _client.PostAsJsonAsync("/api/product", createRequest);
        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        return product!.Id;
    }
}
```

---

### Phase 7: Unit Tests (Optional but Recommended)

**Directory**: `test/UnitTests/UnitTests/`

#### 7.1 ProductDtoMappingTests.cs

**Pattern Reference**: `DtoMappingTests.cs` (lines 1-55)

```csharp
using FluentAssertions;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;

namespace UnitTests;

public class ProductDtoMappingTests
{
    [Fact]
    public void ProductDto_CreateFrom_MapsCorrectly()
    {
        // Arrange
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Stock = 50,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var dto = ProductDto.CreateFrom(product);

        // Assert
        dto.Id.Should().Be(product.Id);
        dto.Name.Should().Be(product.Name);
        dto.Description.Should().Be(product.Description);
        dto.Price.Should().Be(product.Price);
        dto.Stock.Should().Be(product.Stock);
        dto.CreatedAt.Should().Be(product.CreatedAt);
    }
}
```

#### 7.2 OrderDtoMappingTests.cs

```csharp
[Fact]
public void OrderDto_CreateFrom_MapsCorrectly()
{
    // Test Order -> OrderDto mapping with nested OrderItems
}

[Fact]
public void OrderItemDto_CreateFrom_IncludesProductName()
{
    // Test that ProductName is correctly populated from navigation property
}
```

---

### Phase 8: Database Verification & Documentation

#### 8.1 Database Recreation

After model updates, the database must be recreated:

```bash
# Stop existing containers
cd Postgres
docker-compose down

# Remove volume (DESTRUCTIVE - deletes all data)
docker volume rm postgres_postgres-data

# Start fresh
docker-compose up -d

# Verify container is running
docker ps | grep order-payment-db

# Run the application (will recreate DB with new schema)
cd ../../src/OrderPaymentSimulation.Api
dotnet run
```

#### 8.2 Database Verification Commands

```bash
# Connect to database
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation

# Verify tables
\dt

# Check products table structure (should include stock column)
\d products

# Expected output should include:
# stock | integer | not null | default 0

# Check order_items table structure (should include updated_at column)
\d order_items

# Expected output should include:
# updated_at | timestamp | | default CURRENT_TIMESTAMP

# Verify seed data
SELECT id, name, stock FROM products;
SELECT id, product_id, quantity, created_at, updated_at FROM order_items LIMIT 5;

# Verify new OrderStatus value
SELECT DISTINCT status FROM orders;
# Should show: 0 (Pending), 1 (Processing), 2 (Completed), 3 (Cancelled)
# Expired (4) won't show until created

# Exit
\q
```

#### 8.3 Manual API Testing via Swagger

```
1. Start application: dotnet run --launch-profile https
2. Navigate to: https://localhost:7006/swagger
3. Login to get JWT token:
   - POST /api/auth/login
   - Body: { "email": "test@example.com", "password": "Password123!" }
   - Copy token from response

4. Click "Authorize" button in Swagger UI
   - Enter: Bearer {paste-token-here}
   - Click "Authorize"

5. Test Product endpoints:
   - POST /api/product - Create new product
   - GET /api/product - List all products
   - GET /api/product/{id} - Get specific product
   - PUT /api/product/{id} - Update product
   - DELETE /api/product/{id} - Delete product (test both success and conflict cases)

6. Test Order endpoints:
   - POST /api/order - Create order (verify stock decreases)
   - GET /api/order - List your orders
   - GET /api/order/{id} - Get specific order
   - PUT /api/order/{id} - Update order status
   - DELETE /api/order/{id} - Delete pending order (test conflict with non-pending)

7. Verify database changes after each operation
```

#### 8.4 Update README.md

**File**: `README.md` (create if doesn't exist)

Add section documenting new endpoints:

```markdown
## Product Endpoints

### List All Products
```http
GET /api/product
Authorization: Bearer {jwt-token}
```

### Get Product by ID
```http
GET /api/product/{id}
Authorization: Bearer {jwt-token}
```

### Create Product
```http
POST /api/product
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "name": "Laptop",
  "description": "High-performance laptop",
  "price": 999.99,
  "stock": 50
}
```

### Update Product
```http
PUT /api/product/{id}
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "id": 1,
  "name": "Updated Laptop",
  "description": "Updated description",
  "price": 899.99,
  "stock": 45
}
```

### Delete Product
```http
DELETE /api/product/{id}
Authorization: Bearer {jwt-token}
```

## Order Endpoints

### List My Orders
```http
GET /api/order
Authorization: Bearer {jwt-token}
```

### Get Order by ID
```http
GET /api/order/{id}
Authorization: Bearer {jwt-token}
```

### Create Order
```http
POST /api/order
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "items": [
    {
      "productId": 1,
      "quantity": 2
    },
    {
      "productId": 3,
      "quantity": 1
    }
  ]
}
```

**Note**: `total` and `userId` are calculated/set server-side.

### Update Order Status
```http
PUT /api/order/{id}
Authorization: Bearer {jwt-token}
Content-Type: application/json

{
  "id": 1,
  "status": 1
}
```

**Status Values**: 0=Pending, 1=Processing, 2=Completed, 3=Cancelled, 4=Expired

### Delete Order
```http
DELETE /api/order/{id}
Authorization: Bearer {jwt-token}
```

**Note**: Only pending orders (status=0) can be deleted.
```

#### 8.5 Update CLAUDE.md

**File**: `CLAUDE.md`

**Updates Required**:

1. **Project Overview section** - Add mention of Product and Order APIs
2. **Architecture > Technology Stack** - Mark as complete
3. **Project Structure > Controllers** - Add ProductController.cs and OrderController.cs
4. **Project Structure > Dtos** - Add all new DTOs
5. **Entity Models section** - Update Product and OrderItem with new fields
6. **OrderStatus enum** - Document Expired (4) value
7. **Current State > Implemented** - Move Product and Order APIs from Pending
8. **Current State > Pending Implementation** - Remove Product/Order endpoints
9. **Testing section** - Update test counts

**Specific Changes**:

```markdown
### Entity Models

**Product** (`Models/Product.cs`)
- Properties: Id, Name, Description, Price, Stock, CreatedAt  <!-- UPDATED -->
- Relationships: One-to-Many with OrderItems
- Database table: `products`
- Constraints: Name max 100 chars, decimal(10,2) for price, stock >= 0
- Delete behavior: Restrict (prevents deletion if referenced)

**OrderItem** (`Models/OrderItem.cs`)
- Properties: Id, OrderId, ProductId, Quantity, Price, CreatedAt, UpdatedAt  <!-- UPDATED -->
- ...

**OrderStatus** (`Models/OrderStatus.cs`)
- Enum values: Pending (0), Processing (1), Completed (2), Cancelled (3), Expired (4)  <!-- UPDATED -->
- ...

### Controllers

**ProductController** (`Controllers/ProductController.cs`)  <!-- NEW -->
- 5 endpoints: GET all, GET by id, POST create, PUT update, DELETE
- All endpoints protected with JWT authentication
- DELETE checks for OrderItem references (returns 409 if referenced)
- Stock management integrated

**OrderController** (`Controllers/OrderController.cs`)  <!-- NEW -->
- 5 endpoints: GET all (filtered by user), GET by id, POST create, PUT update status, DELETE
- All endpoints protected with JWT authentication and user authorization
- Automatic stock deduction on order creation
- Server-side total calculation from product prices
- Users can only access their own orders

### DTOs

**Product DTOs**:  <!-- NEW -->
- ProductDto - Response DTO
- CreateProductRequest - Create DTO with validation
- UpdateProductRequest - Update DTO with validation

**Order DTOs**:  <!-- NEW -->
- OrderDto - Response DTO with nested OrderItemDto collection
- OrderItemDto - Response DTO including product name
- CreateOrderRequest - Create DTO with nested items array
- CreateOrderItemRequest - Request DTO for order items
- UpdateOrderRequest - Status update DTO

**Current State**

**Implemented:**
- ...
- **Product CRUD API (ProductController.cs)** - 5 endpoints with JWT authentication
- **Order CRUD API (OrderController.cs)** - 5 endpoints with authorization and stock management
- **Product and Order DTOs** - Complete request/response DTOs with validation
- **Stock Management** - Automatic stock deduction on order creation
- **Price Locking** - Order items store price at time of purchase
- **Integration tests** - 24 tests total (9 auth/user + 9 product + 8 order)  <!-- UPDATED COUNT -->
- **Unit tests** - 11 tests total (7 existing + 4 new DTO mapping tests)  <!-- UPDATED COUNT -->

**Pending Implementation:**
- Repository pattern (optional architectural improvement)
- Proper EF Core migrations (replace EnsureCreated)
- Payment processing simulation endpoints  <!-- UPDATED: Removed Product/Order from this list -->
- Error handling middleware and global exception handling
- API versioning
- Rate limiting and request throttling
- Logging and monitoring integration
- Refresh token implementation
- Role-based access control (Admin role)
```

---

## ✅ Validation Gates

### 1. Compilation Check

```bash
# Clean previous build
dotnet clean C:\Mine\order-payment-simulation-api\OrderPaymentSimulation.Api.sln

# Build entire solution
dotnet build C:\Mine\order-payment-simulation-api\OrderPaymentSimulation.Api.sln --configuration Release

# Expected: Build succeeded. 0 Warning(s). 0 Error(s).
```

**Critical**: This must pass before proceeding to tests.

### 2. Integration Tests

```bash
# Run integration tests
dotnet test C:\Mine\order-payment-simulation-api\test\IntegrationTests\IntegrationTests\IntegrationTests.csproj --verbosity normal

# Expected: All tests passed (should see ~24 total tests)
```

### 3. Unit Tests

```bash
# Run unit tests
dotnet test C:\Mine\order-payment-simulation-api\test\UnitTests\UnitTests\UnitTests.csproj --verbosity normal

# Expected: All tests passed (should see ~11 total tests)
```

### 4. Application Startup Check

```bash
# Start application
cd C:\Mine\order-payment-simulation-api\src\OrderPaymentSimulation.Api
dotnet run --launch-profile https

# Expected output should include:
# - "Now listening on: https://localhost:7006"
# - No database errors
# - Seed data initialization successful

# Open browser to: https://localhost:7006/swagger
# Verify ProductController and OrderController appear in Swagger UI
```

### 5. Database Schema Verification

```bash
# Connect to database
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation

# Run checks:
\d products      # Should include 'stock' column
\d order_items   # Should include 'updated_at' column

# Exit
\q
```

### 6. End-to-End API Test

```bash
# Use Swagger UI or curl:

# 1. Login
curl -X POST https://localhost:7006/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!"}' \
  -k

# 2. Create Product (use token from step 1)
curl -X POST https://localhost:7006/api/product \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"name":"Test","description":"Test","price":10.00,"stock":100}' \
  -k

# 3. Create Order (use token and product ID from step 2)
curl -X POST https://localhost:7006/api/order \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"items":[{"productId":1,"quantity":2}]}' \
  -k

# 4. Verify in database that stock decreased
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation -c "SELECT id, name, stock FROM products WHERE id = 1;"
```

---

## 📋 Implementation Task Checklist

Execute in this exact order:

### Phase 1: Foundation (Models & Configuration)
- [ ] Add `Stock` property to `Product.cs` model
- [ ] Add `UpdatedAt` property to `OrderItem.cs` model
- [ ] Add `Expired = 4` to `OrderStatus.cs` enum
- [ ] Update `ProductConfiguration.cs` - add stock column mapping
- [ ] Update `OrderItemConfiguration.cs` - add updated_at column mapping
- [ ] Update `SeedData.cs` - add Stock values to products
- [ ] Update `SeedData.cs` - add UpdatedAt values to order items
- [ ] **STOP**: Drop and recreate database (docker-compose down/up)
- [ ] **VALIDATE**: Run application, verify seed data loads without errors
- [ ] **VALIDATE**: Check database schema with `\d products` and `\d order_items`

### Phase 2: Product Module
- [ ] Create `ProductDto.cs`
- [ ] Create `CreateProductRequest.cs`
- [ ] Create `UpdateProductRequest.cs`
- [ ] Create `ProductController.cs` with all 5 endpoints
- [ ] **VALIDATE**: Build solution - must compile
- [ ] Create `ProductControllerTests.cs` with all 9 tests
- [ ] **VALIDATE**: Run integration tests - all Product tests must pass
- [ ] Create `ProductDtoMappingTests.cs` (optional)
- [ ] **VALIDATE**: Test via Swagger UI - all CRUD operations

### Phase 3: Order Module
- [ ] Create `OrderItemDto.cs`
- [ ] Create `OrderDto.cs`
- [ ] Create `CreateOrderItemRequest.cs`
- [ ] Create `CreateOrderRequest.cs`
- [ ] Create `UpdateOrderRequest.cs`
- [ ] Create `OrderController.cs` with all 5 endpoints
- [ ] **VALIDATE**: Build solution - must compile
- [ ] Create `OrderControllerTests.cs` with all 8 tests
- [ ] **VALIDATE**: Run integration tests - all Order tests must pass
- [ ] Create `OrderDtoMappingTests.cs` (optional)
- [ ] **VALIDATE**: Test via Swagger UI - all CRUD operations

### Phase 4: Verification & Documentation
- [ ] Run all validation gates (compilation, all tests, startup)
- [ ] Perform end-to-end API test (login -> create product -> create order)
- [ ] Verify stock deduction in database
- [ ] Verify authorization (user A cannot access user B's orders)
- [ ] Test edge cases (insufficient stock, delete referenced product, etc.)
- [ ] Update `README.md` with new endpoints
- [ ] Update `CLAUDE.md` with implementation details
- [ ] **FINAL VALIDATE**: All 6 validation gates pass

---

## 🎯 Success Criteria

Implementation is complete when:

1. ✅ All validation gates pass (6/6)
2. ✅ Product CRUD API fully functional (5 endpoints)
3. ✅ Order CRUD API fully functional (5 endpoints)
4. ✅ Stock management working (stock decreases on order creation)
5. ✅ Authorization working (users only see their own orders)
6. ✅ All integration tests pass (~24 total)
7. ✅ All unit tests pass (~11 total)
8. ✅ Database schema updated correctly (stock, updated_at columns exist)
9. ✅ Swagger UI shows all endpoints
10. ✅ Documentation updated (README.md, CLAUDE.md)

---

## ⚠️ Common Pitfalls & Solutions

### 1. Database Schema Not Updated
**Problem**: Running application after model changes shows old schema.
**Solution**: Drop and recreate database:
```bash
cd Postgres
docker-compose down
docker volume rm postgres_postgres-data
docker-compose up -d
```

### 2. Integration Tests Failing Due to In-Memory Database
**Problem**: In-memory DB doesn't enforce some constraints.
**Solution**: Tests are designed to work with in-memory. Ensure `CustomWebApplicationFactory` creates unique DB per test instance.

### 3. JWT Token Not Including Claims
**Problem**: `GetCurrentUserId()` returns 0.
**Solution**: Verify `JwtService.GenerateToken()` includes `ClaimTypes.NameIdentifier`. Check token in https://jwt.io to verify claims.

### 4. Product Delete Fails with Foreign Key Constraint
**Problem**: Database constraint violation instead of 409 Conflict.
**Solution**: Ensure `Include(p => p.OrderItems)` is used in DELETE endpoint to check references before attempting delete.

### 5. Order Total Mismatch
**Problem**: Order total doesn't match sum of items.
**Solution**: Verify server-side calculation uses `product.Price * item.Quantity` and sums all items. Never trust client-provided total.

### 6. Stock Not Decreasing
**Problem**: Stock stays the same after order creation.
**Solution**: Ensure `product.Stock -= item.Quantity` is called in order creation loop, and `SaveChangesAsync()` is called after.

### 7. User Can Access Other User's Orders
**Problem**: Authorization not working.
**Solution**: Ensure all Order endpoints include:
```csharp
if (order.UserId != GetCurrentUserId())
    return Forbid();
```

### 8. Tests Failing Due to Unique Email Constraint
**Problem**: Integration tests fail on second run.
**Solution**: Use `$"user{Guid.NewGuid()}@example.com"` pattern for unique emails per test run.

---

## 📊 Confidence Score: 9/10

**Why 9/10?**

✅ **Strengths**:
- Clear existing patterns to follow (UserController as blueprint)
- Well-defined requirements with specific field types
- Existing test infrastructure (xUnit, FluentAssertions)
- Concrete examples for every component
- Validation gates are executable and clear

⚠️ **Risks** (reducing from 10 to 9):
- Database recreation required (manual step, could be missed)
- Stock management adds complexity to order creation
- Authorization logic is critical and easy to miss
- OrderStatus enum decision requires judgment call

**Mitigation**:
- Clear task checklist with validation steps after each phase
- Pseudocode includes all critical authorization checks
- Explicit warnings about database recreation
- Common pitfalls section addresses likely issues

**Expected Result**: One-pass implementation success if task checklist is followed sequentially with validation gates.

---

## 🔗 Quick Reference Links

**Internal Files**:
- UserController Pattern: `src/OrderPaymentSimulation.Api/Controllers/UserController.cs`
- DTO Pattern: `src/OrderPaymentSimulation.Api/Dtos/UserDto.cs`
- Configuration Pattern: `src/OrderPaymentSimulation.Api/Data/Configurations/UserConfiguration.cs`
- Test Pattern: `test/IntegrationTests/IntegrationTests/UserControllerTests.cs`

**External Documentation**:
- ASP.NET Core Web API: https://learn.microsoft.com/en-us/aspnet/core/web-api/
- EF Core Fluent API: https://learn.microsoft.com/en-us/ef/core/modeling/
- Data Annotations: https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
- xUnit: https://xunit.net/
- FluentAssertions: https://fluentassertions.com/

---

*End of PRP - Ready for implementation*
