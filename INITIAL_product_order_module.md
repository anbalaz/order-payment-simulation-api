# Feature: Product and Order CRUD REST APIs

## Product Controller Requirements

### Product Entity Model
Create a new Product controller with the following requirements:

**Product Fields** (note: existing Product model at `src/OrderPaymentSimulation.Api/Models/Product.cs:1-13` is missing the `stock` field):
- `id` - Primary key (int)
- `name` - String, max length 100 (required)
- `description` - String (nullable)
- `price` - Decimal >= 0 (required)
- `stock` - Integer >= 0 (NEW FIELD - must be added to existing Product model)
- `created_at` - Timestamp (UTC)

**Note**: The existing Product model at `src/OrderPaymentSimulation.Api/Models/Product.cs` currently has: `Id`, `Name`, `Description`, `Price`, and `CreatedAt`. You will need to ADD the `Stock` property.

**Entity Configuration**: Update `src/OrderPaymentSimulation.Api/Data/Configurations/ProductConfiguration.cs:1-40` to include the new `stock` field with proper column mapping following the existing pattern (snake_case column name, required constraint, validation >= 0).

### Product Controller
Create `src/OrderPaymentSimulation.Api/Controllers/ProductController.cs` with **5 CRUD endpoints**:
1. `GET /api/product` - List all products
2. `GET /api/product/{id}` - Get product by ID
3. `POST /api/product` - Create new product
4. `PUT /api/product/{id}` - Update existing product
5. `DELETE /api/product/{id}` - Delete existing product

**Authentication**: ALL endpoints must be protected with `[Authorize]` attribute (JWT Bearer Token required).

**Reference Pattern**: Follow the structure from `src/OrderPaymentSimulation.Api/Controllers/UserController.cs:1-235`:
- Use `[ApiController]` and `[Route("api/[controller]")]` attributes
- Inject `OrderPaymentDbContext` and `ILogger<ProductController>` via constructor
- Use `[ProducesResponseType]` attributes for Swagger documentation (lines 43-45, 97-101, etc.)
- Return `ActionResult<T>` for responses
- Implement proper error handling with try-catch blocks
- Log operations using injected logger
- Validate `ModelState` before processing requests (line 50)
- Return appropriate HTTP status codes: 200 OK, 201 Created, 400 BadRequest, 401 Unauthorized, 404 NotFound, 500 InternalServerError

### Product DTOs
Create DTOs in `src/OrderPaymentSimulation.Api/Dtos/` folder:
- `ProductDto.cs` - Response DTO with all product fields
- `CreateProductRequest.cs` - Request DTO for creating products
- `UpdateProductRequest.cs` - Request DTO for updating products

**Mapping Pattern**: Use the static factory method pattern from `src/OrderPaymentSimulation.Api/Dtos/UserDto.cs:16-24`:
```csharp
public static ProductDto CreateFrom(Product product)
    => new()
    {
        Id = product.Id,
        Name = product.Name,
        // ... map other fields
    };
```

**Validation**: Add Data Annotations on request DTOs:
- `[Required]` for mandatory fields
- `[MaxLength(100)]` for name field
- `[Range(0, double.MaxValue)]` for price and stock to ensure >= 0

### Input Validation
Validate all input DTOs. If validation fails, return `400 Bad Request` with `ModelState` errors (see `UserController.cs:50-53`).

---

## Order Controller Requirements

### Order Entity Model
**Note**: The existing Order model at `src/OrderPaymentSimulation.Api/Models/Order.cs:1-15` already matches most requirements, but verify the status enum.

**Order Fields**:
- `id` - Primary key (int)
- `user_id` - Foreign key to User (int)
- `total` - Decimal >= 0 (required)
- `status` - Enum: `pending`, `processing`, `completed`, `expired` (see note below)
- `created_at` - Timestamp (UTC)
- `updated_at` - Timestamp (UTC)

**IMPORTANT STATUS ENUM DISCREPANCY**:
- Current `OrderStatus` enum at `src/OrderPaymentSimulation.Api/Models/OrderStatus.cs:3-8` has values: `Pending`, `Processing`, `Completed`, `Cancelled` (line 8)
- Requirements specify: `pending`, `processing`, `completed`, `expired`
- **Decision needed**: Should `Cancelled` be replaced with `Expired`, or should `Expired` be added as a 5th status?

**Order Items** (related entity `OrderItem.cs:1-15`):
- `id` - Primary key (int)
- `product_id` - Foreign key to Product (int)
- `quantity` - Integer > 0 (required)
- `price` - Decimal > 0 (required)
- `created_at` - Timestamp (UTC)
- `updated_at` - NEW FIELD (must be added to existing OrderItem model)

**Note**: The existing OrderItem model is missing the `updated_at` field. You will need to add this property.

### Order Controller
Create `src/OrderPaymentSimulation.Api/Controllers/OrderController.cs` with **5 CRUD endpoints**:
1. `GET /api/order` - List all orders (consider filtering by current user's orders)
2. `GET /api/order/{id}` - Get order by ID with order items
3. `POST /api/order` - Create new order with order items
4. `PUT /api/order/{id}` - Update order (e.g., status updates)
4. `DELETE /api/order/{id}` - DELETE order

**Authentication**: ALL endpoints must be protected with `[Authorize]` attribute (JWT Bearer Token required).

**Authorization Considerations**:
- Users should only see their own orders (filter by `UserId` matching JWT claim)
- Use the `GetCurrentUserId()` pattern from `UserController.cs:32-36`:
```csharp
private int GetCurrentUserId()
{
    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return int.TryParse(userIdClaim, out var userId) ? userId : 0;
}
```

**Reference Pattern**: Follow `UserController.cs:1-235` structure as described in Product Controller section.

### Order DTOs
Create DTOs in `src/OrderPaymentSimulation.Api/Dtos/` folder:
- `OrderDto.cs` - Response DTO including nested `OrderItemDto` collection
- `OrderItemDto.cs` - Response DTO for order items (include product details)
- `CreateOrderRequest.cs` - Request DTO with nested items array
- `CreateOrderItemRequest.cs` - Request DTO for individual order items
- `UpdateOrderRequest.cs` - Request DTO for updating orders (e.g., status changes)

**Mapping Pattern**: Use static factory methods like `UserDto.CreateFrom()` at `UserDto.cs:16-24`.

**Validation Rules**:
- Order total must be >= 0
- Order items quantity must be > 0
- Order items price must be > 0
- Include foreign key validations (valid user_id, product_id)

---

## Testing Requirements

### Integration Tests
Add integration tests to `test/IntegrationTests/IntegrationTests/IntegrationTests.csproj`:
- Create `ProductControllerTests.cs`
- Create `OrderControllerTests.cs`

**Test Pattern Reference**: Follow `test/IntegrationTests/IntegrationTests/UserControllerTests.cs:1-170`:
- Use `IClassFixture<CustomWebApplicationFactory>` (line 13)
- Create HttpClient via factory (line 21)
- Use FluentAssertions for assertions (line 4, example at line 39)
- Test authentication flows with JWT tokens (lines 76-90, 145-169)
- Test unauthorized access attempts (lines 93-100)
- Verify database state after operations (lines 139-142)
- Use unique email generation pattern: `$"user{Guid.NewGuid()}@example.com"` (line 49)

**Required Test Cases** (minimum):
For ProductController:
1. Create product with valid data → 201 Created
2. Create product with invalid data (negative price/stock) → 400 BadRequest
3. Get product by ID with authentication → 200 OK
4. Get product without authentication → 401 Unauthorized
5. Update product with valid data → 200 OK
6. Get all products → 200 OK

For OrderController:
1. Create order with valid items → 201 Created
2. Create order with invalid items (quantity <= 0) → 400 BadRequest
3. Get order by ID (own order) → 200 OK
4. Get order by ID (other user's order) → 401/403
5. Update order status → 200 OK
6. List user's orders → 200 OK

### Unit Tests
Add unit tests to `test/UnitTests/UnitTests/UnitTests.csproj` if necessary:
- DTO mapping logic tests
- Validation logic tests
- Business rules tests

**Test Pattern Reference**: Use xUnit, AutoFixture, and Moq as shown in existing unit tests.

**Technology Stack** (per requirements):
- xUnit - Test framework
- AutoFixture (https://www.nuget.org/packages/autofixture) - Test data generation
- Moq (https://www.nuget.org/packages/moq/) - Mocking framework
- FluentAssertions - Assertion library (already in use, see `UserControllerTests.cs:4`)

---

## Additional HTTP Status Codes

Beyond the standard codes used in UserController (200, 201, 400, 401, 404, 500), consider adding:
- `403 Forbidden` - When user tries to access another user's order
- `409 Conflict` - When trying to update order in invalid state transition
- `422 Unprocessable Entity` - When business rules prevent the operation (e.g., insufficient stock)

---

## Documentation Updates

### Update README.md
Document the new endpoints:
- Product CRUD operations with example requests
- Order CRUD operations with example requests
- Authentication requirements for all endpoints

### Update CLAUDE.md
After successful implementation, update `CLAUDE.md` with:
- New Product and Order controller documentation
- Updated entity models with new fields (Product.Stock, OrderItem.UpdatedAt)
- Updated OrderStatus enum values
- New DTO documentation
- New test coverage information
- Updated "Current State" section to reflect implemented features
- Remove Product and Order endpoints from "Pending Implementation" section

---

## Database Verification

After implementation, verify data persistence in PostgreSQL:

**Connection Details** (from `CLAUDE.md`):
```bash
# Connect to PostgreSQL container
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation

# Verify tables exist
\dt

# Check products table structure
\d products

# Check orders table structure
\d orders

# Check order_items table structure
\d order_items
```

**Test Data Operations**:
1. Create products via `POST /api/product` endpoint
2. Query products table: `SELECT * FROM products;`
3. Create orders via `POST /api/order` endpoint
4. Query orders and order_items tables to verify cascade behavior
5. Update order status via `PUT /api/order/{id}` endpoint
6. Verify updated_at timestamp changes
7. Test delete operations and verify restrict/cascade behaviors

**Expected Database Behaviors** (from existing configurations):
- Product deletion should be RESTRICTED if referenced by order_items (`ProductConfiguration.cs:38`)
- Order deletion should CASCADE to order_items
- Timestamps (created_at, updated_at) should auto-populate

---

## EXAMPLES

### Entity Model Pattern
**Reference**: User model at `src/OrderPaymentSimulation.Api/Models/User.cs:1-14`
```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; } // NEW FIELD
    // ... navigation properties
}
```

### Entity Configuration Pattern
**Reference**: `src/OrderPaymentSimulation.Api/Data/Configurations/UserConfiguration.cs` (apply same pattern to ProductConfiguration and OrderItemConfiguration)
- Use `IEntityTypeConfiguration<T>` interface
- Snake_case for table and column names
- Register in DbContext via `modelBuilder.ApplyConfiguration()`

### Controller Pattern
**Reference**: `src/OrderPaymentSimulation.Api/Controllers/UserController.cs:1-235`
- Lines 14-27: Controller setup with DI
- Lines 32-36: JWT claims helper method
- Lines 46-90: Create endpoint with ModelState validation, error handling
- Lines 102-161: Update endpoint with authorization checks
- Lines 172-190: Get endpoint
- Lines 201-234: Delete endpoint

### DTO Pattern
**Reference**: `src/OrderPaymentSimulation.Api/Dtos/UserDto.cs:1-25`
- Lines 8-14: DTO properties
- Lines 16-24: Static `CreateFrom()` factory method

### DTO Request Pattern
**Reference**: `src/OrderPaymentSimulation.Api/Dtos/CreateUserRequest.cs` and `UpdateUserRequest.cs`
- Use Data Annotations for validation (`[Required]`, `[EmailAddress]`, `[MaxLength]`)

### Integration Test Pattern
**Reference**: `test/IntegrationTests/IntegrationTests/UserControllerTests.cs:1-170`
- Lines 13-22: Test class setup with factory
- Lines 24-43: Basic CRUD test with FluentAssertions
- Lines 45-73: Duplicate/validation test
- Lines 75-90: Authenticated endpoint test
- Lines 92-100: Unauthorized access test
- Lines 145-169: Helper method for creating authenticated user

---

## DOCUMENTATION

### External Resources
- AutoFixture NuGet: https://www.nuget.org/packages/autofixture
- Moq NuGet: https://www.nuget.org/packages/moq/

### Internal References
- Project overview: `CLAUDE.md` (entire file provides architecture context)
- Existing User CRUD implementation: `src/OrderPaymentSimulation.Api/Controllers/UserController.cs:1-235`
- Database context: `src/OrderPaymentSimulation.Api/Data/OrderPaymentDbContext.cs`
- Existing configurations: `src/OrderPaymentSimulation.Api/Data/Configurations/`
- JWT authentication setup: `src/OrderPaymentSimulation.Api/Program.cs` (authentication middleware configuration)

### Database Schema Inspection
Use MCP server for postgres to inspect tables structure and verify data format after implementation.

---

## OTHER CONSIDERATIONS

### Response Wrapping
Use `ActionResult<T>` from `Microsoft.AspNetCore.Mvc` for wrapping response bodies (see `UserController.cs:46, 102, 172` for examples).

### Authentication Requirement
All new endpoints must be protected with JWT Bearer Token. Use `[Authorize]` attribute at controller or method level (see `UserController.cs:96, 167, 196`).

**Exception**: If you want to allow public product browsing, you can use `[AllowAnonymous]` on GET endpoints (similar to `UserController.cs:42` for user registration).

### Model Separation
**CRITICAL**: Do NOT use the same model for controller (DTO) and database (Entity). Follow the existing pattern:
- **Entity Models**: `src/OrderPaymentSimulation.Api/Models/` (e.g., `User.cs`, `Product.cs`, `Order.cs`)
- **DTOs**: `src/OrderPaymentSimulation.Api/Dtos/` (e.g., `UserDto.cs`, `CreateUserRequest.cs`)
- Map between entities and DTOs using static factory methods (see `UserDto.cs:16-24`)

### Swagger Documentation
The project uses Swagger (Swashbuckle) for API documentation with JWT Bearer support. Ensure all new endpoints:
- Have XML documentation comments (see `UserController.cs:29, 38, 92, 164, 193`)
- Use `[ProducesResponseType]` attributes for documenting possible responses
- Are automatically included in Swagger UI at `/swagger`

### JWT Configuration
JWT settings are in `appsettings.json`. Use `Microsoft.IdentityModel.JsonWebTokens` or existing JWT infrastructure (see `src/OrderPaymentSimulation.Api/Services/JwtService.cs`).

### Database Storage
Products and orders should persist to the existing PostgreSQL database (`order_payment_simulation`). The database connection is already configured in `appsettings.json` and `Program.cs`.

**Database Setup**:
```bash
cd Postgres
docker-compose up -d
```

---

## Implementation Checklist

- [ ] Add `Stock` property to `Product.cs` model
- [ ] Add `UpdatedAt` property to `OrderItem.cs` model
- [ ] Resolve OrderStatus enum discrepancy (Cancelled vs Expired)
- [ ] Update `ProductConfiguration.cs` to include stock field mapping
- [ ] Update `OrderItemConfiguration.cs` to include updated_at field mapping
- [ ] Create ProductDto, CreateProductRequest, UpdateProductRequest DTOs
- [ ] Create OrderDto, OrderItemDto, CreateOrderRequest, CreateOrderItemRequest, UpdateOrderRequest DTOs
- [ ] Implement ProductController with 4 CRUD endpoints
- [ ] Implement OrderController with 4 CRUD endpoints
- [ ] Add JWT authorization to all new endpoints
- [ ] Create ProductControllerTests with minimum 6 test cases
- [ ] Create OrderControllerTests with minimum 6 test cases
- [ ] Add unit tests if necessary
- [ ] Test all endpoints via Swagger UI
- [ ] Verify database changes in PostgreSQL
- [ ] Update README.md with new features
- [ ] Update CLAUDE.md with implementation details
