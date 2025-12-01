# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASP.NET Core 8.0 Web API for simulating order payment workflows with PostgreSQL database integration. The project includes a complete entity model for users, products, orders, and order items with Entity Framework Core.

## Build and Run Commands

```bash
# Build the solution
dotnet build OrderPaymentSimulation.Api.sln

# Run the API (from project directory)
cd src/OrderPaymentSimulation.Api
dotnet run

# Run with specific profile
dotnet run --launch-profile https  # Runs on https://localhost:7006
dotnet run --launch-profile http   # Runs on http://localhost:5267

# Restore dependencies
dotnet restore OrderPaymentSimulation.Api.sln

# Clean build artifacts
dotnet clean OrderPaymentSimulation.Api.sln
```

## Database Setup

### PostgreSQL with Docker

```bash
# Start PostgreSQL container
cd Postgres
docker-compose up -d

# Stop PostgreSQL container
docker-compose down

# View PostgreSQL logs
docker-compose logs -f postgres

# Connect to PostgreSQL
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation
```

**Database Configuration:**
- Host: localhost
- Port: 5432
- Database: order_payment_simulation
- User: orderuser
- Password: dev_password
- Connection string configured in `appsettings.json`

### Database Initialization

The application automatically:
1. Creates database schema on startup using `EnsureCreated()` (located in Program.cs)
2. Seeds initial data via `SeedData.Initialize()` including:
   - 2 test users with hashed passwords
   - 5 sample products (Laptop, Mouse, Keyboard, Monitor, Headphones)
   - 3 sample orders with different statuses

**Note:** Currently using `EnsureCreated()` for development. For production, implement proper EF Core migrations.

## Architecture

### Technology Stack
- .NET 8.0 (target framework: net8.0)
- ASP.NET Core Web API with Controller-based APIs
- Entity Framework Core 8.0 with PostgreSQL (Npgsql 8.0.4)
- JWT Authentication (Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11)
- Swagger/OpenAPI (Swashbuckle 6.6.2) for API documentation with JWT Bearer support
- Docker for PostgreSQL containerization
- ASP.NET Identity PasswordHasher for password hashing
- xUnit, FluentAssertions, AutoFixture, and Moq for testing

### Project Structure

```
order-payment-simulation-api/
├── src/OrderPaymentSimulation.Api/
│   ├── Controllers/          # API Controllers
│   │   ├── AuthController.cs            # JWT authentication endpoint
│   │   └── UserController.cs            # User CRUD endpoints
│   ├── Data/                 # Database context and configurations
│   │   ├── Configurations/   # Entity Type Configurations (Fluent API)
│   │   │   ├── OrderConfiguration.cs
│   │   │   ├── OrderItemConfiguration.cs
│   │   │   ├── ProductConfiguration.cs
│   │   │   └── UserConfiguration.cs
│   │   ├── OrderPaymentDbContext.cs
│   │   └── SeedData.cs
│   ├── Dtos/                 # Data Transfer Objects
│   │   ├── CreateUserRequest.cs         # User creation DTO
│   │   ├── UpdateUserRequest.cs         # User update DTO
│   │   ├── UserDto.cs                   # User response DTO
│   │   ├── LoginRequest.cs              # Login request DTO
│   │   └── LoginResponse.cs             # Login response with JWT
│   ├── Models/               # Domain entities
│   │   ├── Order.cs
│   │   ├── OrderItem.cs
│   │   ├── OrderStatus.cs (enum)
│   │   ├── Product.cs
│   │   └── User.cs
│   ├── Services/             # Application services
│   │   ├── IJwtService.cs               # JWT generation interface
│   │   └── JwtService.cs                # JWT generation implementation
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Program.cs            # Entry point with auth configuration
│   ├── appsettings.json      # Configuration with JWT settings
│   └── OrderPaymentSimulation.Api.csproj
├── test/
│   ├── IntegrationTests/IntegrationTests/
│   │   ├── AuthControllerTests.cs       # Login endpoint tests (3 tests)
│   │   ├── UserControllerTests.cs       # User CRUD tests (6 tests)
│   │   ├── CustomWebApplicationFactory.cs
│   │   └── IntegrationTests.csproj
│   └── UnitTests/UnitTests/
│       ├── JwtServiceTests.cs           # JWT generation tests (2 tests)
│       ├── PasswordHashingTests.cs      # Password hashing tests (3 tests)
│       ├── DtoMappingTests.cs           # DTO mapping tests (2 tests)
│       └── UnitTests.csproj
├── Postgres/
│   ├── docker-compose.yml    # PostgreSQL container setup
│   └── init-scripts/
└── OrderPaymentSimulation.Api.sln
```

### Entity Models

**User** (`Models/User.cs`)
- Properties: Id, Name, Email, Password, CreatedAt, UpdatedAt
- Relationships: One-to-Many with Orders
- Database table: `users`
- Constraints: Unique index on email, name max 100 chars

**Product** (`Models/Product.cs`)
- Properties: Id, Name, Description, Price, CreatedAt
- Relationships: One-to-Many with OrderItems
- Database table: `products`
- Constraints: Name max 100 chars, decimal(10,2) for price
- Delete behavior: Restrict (prevents deletion if referenced)

**Order** (`Models/Order.cs`)
- Properties: Id, UserId, Total, Status, CreatedAt, UpdatedAt
- Relationships: Many-to-One with User, One-to-Many with OrderItems
- Database table: `orders`
- Constraints: Index on status field
- Delete behavior: Cascade from User, Cascade to OrderItems

**OrderItem** (`Models/OrderItem.cs`)
- Properties: Id, OrderId, ProductId, Quantity, Price, CreatedAt
- Relationships: Many-to-One with Order and Product
- Database table: `order_items`
- Constraints: Indexes on order_id and product_id

**OrderStatus** (`Models/OrderStatus.cs`)
- Enum values: Pending (0), Processing (1), Completed (2), Cancelled (3)
- Stored as short in database

### Database Configuration Pattern

Entity configurations use the **IEntityTypeConfiguration** pattern in `Data/Configurations/`:
- Fluent API for all entity mappings
- Snake_case table and column names
- Proper relationship configuration with foreign keys
- Default values for timestamps (`CURRENT_TIMESTAMP`)
- Strategic indexes for query performance

**Example Pattern:**
```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name).HasColumnName("name").IsRequired().HasMaxLength(100);
        // ... more configuration
    }
}
```

### Configuration

**Development Environment:**
- HTTP: http://localhost:5267
- HTTPS: https://localhost:7006
- Swagger UI: `/swagger` endpoint (Development only)

**Middleware Pipeline** (Program.cs):
1. DbContext registration with PostgreSQL
2. Controller services
3. JWT Authentication configuration with Bearer scheme
4. Swagger/OpenAPI with JWT Bearer security
5. Database initialization (EnsureCreated + SeedData)
6. HTTPS redirection
7. Authentication middleware (`app.UseAuthentication()`)
8. Authorization middleware (`app.UseAuthorization()`)
9. Controller mapping

### Root Namespace
`order_payment_simulation_api`

### Authentication & Authorization

**JWT Configuration** (appsettings.json):
- Algorithm: HS256 (HMAC-SHA256)
- Token Expiry: 60 minutes (configurable via `Jwt:ExpiryMinutes`)
- Issuer: OrderPaymentSimulation
- Audience: OrderPaymentSimulation
- Signing Key: Configured in `Jwt:Key` (minimum 32 characters for HS256)

**JWT Claims Structure**:
- `ClaimTypes.NameIdentifier` (nameid): User ID
- `ClaimTypes.Name` (unique_name): User full name
- `ClaimTypes.Email` (email): User email address
- `JwtRegisteredClaimNames.Sub`: User ID
- `JwtRegisteredClaimNames.Email`: User email
- `JwtRegisteredClaimNames.Jti`: Unique token identifier (GUID)

**Authorization Rules**:
- **Public Endpoints (AllowAnonymous)**:
  - `POST /api/auth/login` - User authentication
  - `PUT /api/user` - User registration

- **Protected Endpoints (Require JWT Bearer Token)**:
  - `GET /api/user/{id}` - Retrieve user by ID (any authenticated user)
  - `POST /api/user` - Update user (users can only update their own profile)
  - `DELETE /api/user/{id}` - Delete user (users can only delete their own account)

**Security Implementation**:
- Password hashing using `PasswordHasher<User>` with bcrypt algorithm
- Token validation on every protected request
- ClockSkew set to TimeSpan.Zero (no tolerance for expired tokens)
- Bearer token transmitted via `Authorization: Bearer {token}` header

## Current State

**Implemented:**
- Complete PostgreSQL database setup with Docker
- Entity Framework Core with 4 domain models
- Fluent API entity configurations
- Database seeding with test data
- Password hashing using ASP.NET Identity
- Proper entity relationships with cascade/restrict behaviors
- Database indexes on frequently queried fields
- Snake_case database naming convention
- **JWT Authentication with Bearer token validation**
- **User CRUD API (UserController.cs)** - Create, Read, Update, Delete endpoints with authorization
- **Authentication API (AuthController.cs)** - Login endpoint with JWT token generation
- **DTOs for API contracts** - Separation of domain models from API requests/responses
- **Authorization middleware** - JWT Bearer authentication with claims-based authorization
- **Input validation** - Data Annotations on DTOs for request validation
- **Integration tests** - 9 tests covering authentication and user CRUD workflows
- **Unit tests** - 7 tests for JWT generation, password hashing, and DTO mapping
- **Swagger UI with JWT Bearer support** - Interactive API documentation with authentication
- **Service layer** - IJwtService for JWT token generation

**Pending Implementation:**
- Domain-specific API controllers for Product, Order, and OrderItem entities
- Repository pattern (optional architectural improvement)
- Proper EF Core migrations (replace EnsureCreated)
- Product, Order, and OrderItem DTOs
- Order management endpoints with business logic
- Payment processing simulation endpoints
- Error handling middleware and global exception handling
- API versioning
- Rate limiting and request throttling
- Logging and monitoring integration

**Known Limitations:**
- Using `EnsureCreated()` instead of migrations (not production-ready)
- Only User CRUD endpoints implemented - Product, Order, and OrderItem endpoints pending
- No global exception handling middleware (returns default ASP.NET error responses)
- JWT secret key stored in appsettings.json (should use environment variables or secrets manager in production)
- No refresh token implementation (tokens expire after 60 minutes, requiring re-login)
- Authorization checks are basic (users can only manage their own data, no role-based access control)
- Integration tests use in-memory database instead of test PostgreSQL instance
- No API rate limiting or request throttling

## Development Guidelines

### Naming Conventions
- **C# Code:** PascalCase for classes, properties, methods
- **Database:** snake_case for tables and columns
- **Controllers:** Attribute routing pattern `[Route("[controller]")]`

### Entity Configuration Rules
1. Always use `IEntityTypeConfiguration<T>` for entity configuration
2. Place configurations in `Data/Configurations/` folder
3. Register via `modelBuilder.ApplyConfiguration()` in DbContext
4. Use snake_case for all database identifiers
5. Set appropriate delete behaviors (Cascade, Restrict, SetNull)
6. Add indexes on foreign keys and frequently queried fields

### Password Handling
- Use `PasswordHasher<User>` from Microsoft.AspNetCore.Identity
- Never store plain text passwords
- Hash passwords before saving to database

### Testing Approach
**Integration Tests** (`test/IntegrationTests/IntegrationTests/`):
- Use `WebApplicationFactory<Program>` for in-memory testing
- Custom factory with unique in-memory database per test instance
- Test full HTTP request/response cycles through controllers
- Cover authentication flows and protected endpoint authorization
- Current coverage: 9 tests (AuthControllerTests, UserControllerTests)

**Unit Tests** (`test/UnitTests/UnitTests/`):
- Use xUnit with FluentAssertions for readable assertions
- Use AutoFixture for test data generation
- Use Moq for mocking dependencies (when needed)
- Test business logic in isolation (JWT service, password hashing, DTOs)
- Current coverage: 7 tests (JwtServiceTests, PasswordHashingTests, DtoMappingTests)

**Running Tests**:
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test test/IntegrationTests/IntegrationTests/IntegrationTests.csproj
dotnet test test/UnitTests/UnitTests/UnitTests.csproj
```

## Next Development Steps

1. **Implement proper EF Core migrations**
   ```bash
   cd src/OrderPaymentSimulation.Api
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   ```
   Replace `EnsureCreated()` in Program.cs with `context.Database.Migrate()`

2. **Create Product API endpoints** (ProductController)
   - GET /api/product - List all products
   - GET /api/product/{id} - Get product by ID
   - POST /api/product - Create product (admin only)
   - PUT /api/product/{id} - Update product (admin only)
   - DELETE /api/product/{id} - Delete product (admin only)

3. **Create Order API endpoints** (OrderController)
   - GET /api/order - List user's orders
   - GET /api/order/{id} - Get order details
   - POST /api/order - Create new order
   - PUT /api/order/{id}/status - Update order status
   - DELETE /api/order/{id} - Cancel order

4. **Implement payment simulation logic**
   - POST /api/payment/process - Simulate payment processing
   - Add payment status tracking to orders
   - Implement order status workflow (Pending → Processing → Completed/Cancelled)

5. **Add role-based authorization**
   - Extend JWT claims with roles (Admin, User)
   - Implement role checks in controllers using `[Authorize(Roles = "Admin")]`
   - Update seed data with admin user

6. **Add global exception handling middleware**
   - Create custom exception handler middleware
   - Return consistent error response format
   - Log exceptions appropriately

## Useful Commands

```bash
# Entity Framework Core
dotnet ef migrations add <MigrationName>
dotnet ef database update
dotnet ef migrations list
dotnet ef database drop --force

# Docker PostgreSQL
docker-compose -f Postgres/docker-compose.yml up -d
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation

# Testing
dotnet test
dotnet test --filter "Category=Integration"
```
