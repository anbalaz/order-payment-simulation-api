## FEATURE:

New controller for User. That would support CRUD operations. Model of user: id, name (max length 100), email (unique), password.

Controller Should have 4 endpoints,

PUT api/user (create)
POST api/user (update),
GET api/user (get),
DELETE api/user (delete).

Http responses for user
201 for created (PUT)
Validate inputs if not valid return 400. If valid create/update/get/delete data in db if the endpoint requires it.
401 for unauthorized (when no jwt token or token that has no right over user)
200 OK for get, returns data about user (Id, name, email, createdAt, updatedAt)
500 if unexpected error occurs.
add other if you consider it necessary

New authentication controller for Login, should follow REST API
checks user credentials (email, password) and if correct, return JWT Token

for invalid credentials in login return 401

add integration tests into new IntegrationTests project that is in new folder test in root of the project example (.\test\IntegrationTests\IntegrationTests.csproj).
if necessary add unit tests into new UnitTests project that is in new folder test in root of the project example (.\test\UnitTests\UnitTests.csproj).

For tests use x-unit tests and also autofixture (https://www.nuget.org/packages/autofixture), Moq (https://www.nuget.org/packages/moq/)

update Readme about new features.

Remove weather controller with all its linked structure and data, It is no longer needed.

At the end check if data in postgre db is changed when you use endpoints accordingly

after everything works update Claude.md

## PROJECT CONTEXT:

**Existing Project Structure:**
- Solution: `OrderPaymentSimulation.Api.sln` (root)
- Main API: `src/OrderPaymentSimulation.Api/` (note: single-level directory structure)
- Root namespace: `order_payment_simulation_api`
- Framework: .NET 8.0
- Database: PostgreSQL 16 (Docker container: `order-payment-db`)

**Current Database & Entity Setup:**
The User model already exists with proper configuration:

1. **User Model** (`src/OrderPaymentSimulation.Api/Models/User.cs:1-14`)
   - Properties: Id, Name, Email, Password, CreatedAt, UpdatedAt
   - Navigation: ICollection<Order> Orders
   - Password hashing already implemented in SeedData using `PasswordHasher<User>` from Microsoft.AspNetCore.Identity

2. **User Database Configuration** (`src/OrderPaymentSimulation.Api/Data/Configurations/UserConfiguration.cs:1-48`)
   - Table: `users` (snake_case)
   - Columns: id, name, email, password, created_at, updated_at
   - Constraints: email unique index (idx_users_email), name max 100, email max 100
   - Default timestamps: CURRENT_TIMESTAMP
   - Cascade delete to Orders

3. **Database Context** (`src/OrderPaymentSimulation.Api/Data/OrderPaymentDbContext.cs`)
   - Already configured with PostgreSQL via Npgsql
   - Uses IEntityTypeConfiguration pattern
   - Connection string in appsettings.json

4. **Seed Data** (`src/OrderPaymentSimulation.Api/Data/SeedData.cs:1-110`)
   - 2 test users with hashed passwords:
     - admin@example.com / Password123!
     - test@example.com / Password123!
   - Password hashing pattern at line 16-40 shows how to hash/verify

**Controller Pattern:**
See existing controller for reference:
- `src/OrderPaymentSimulation.Api/Controllers/WeatherForecastController.cs:1-32`
  - Uses `[ApiController]` and `[Route("[controller]")]` attributes
  - Inherits from ControllerBase
  - Namespace: `order_payment_simulation_api.Controllers`

**Middleware & Configuration** (`src/OrderPaymentSimulation.Api/Program.cs:1-52`):
- DbContext registered at line 12-13
- Controllers added at line 15
- Swagger configured at line 17-18
- Authorization middleware exists at line 48 (but not configured yet)
- Database initialization at line 29-30

**Database Connection Details:**
- Host: localhost:5432
- Database: order_payment_simulation
- User: orderuser
- Password: dev_password
- Docker container: order-payment-db
- Compose file: `Postgres/docker-compose.yml`

**Template Code to Remove:**
- `src/OrderPaymentSimulation.Api/Controllers/WeatherForecastController.cs:1-32`
- `src/OrderPaymentSimulation.Api/WeatherForecast.cs`

## EXAMPLES:

**User Model (Already Exists):**
Located at `src/OrderPaymentSimulation.Api/Models/User.cs:1-14`:
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

**UserDto to Create:**
Create new folder `src/OrderPaymentSimulation.Api/Dtos/` and add UserDto:
```csharp
namespace order_payment_simulation_api.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Static mapping method (manual mapping, no AutoMapper)
    public static UserDto CreateFrom(User user)
        => new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
}
```

**Note:** UserDto should NOT include Password field for security reasons (only used in request DTOs, not response)

**Additional DTOs to Create:**
```csharp
// For user creation (PUT api/user)
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// For user update (POST api/user)
public class UpdateUserRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; } // Optional for update
}

// For login (POST api/auth/login)
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}
```

**Controller Pattern (based on WeatherForecastController.cs:1-32):**
Create `src/OrderPaymentSimulation.Api/Controllers/UserController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using order_payment_simulation_api.Dtos;

namespace order_payment_simulation_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly OrderPaymentDbContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(OrderPaymentDbContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPut]
    // [AllowAnonymous] - Allow user creation without authentication
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        // Validate, hash password, save to DB
        // Return 201 Created with UserDto
    }

    [HttpPost]
    [Authorize] // Requires JWT token
    public async Task<IActionResult> Update([FromBody] UpdateUserRequest request)
    {
        // Validate JWT, check authorization, update user
        // Return 200 OK with updated UserDto
    }

    // ... GET and DELETE endpoints
}
```

**Password Hashing Pattern (from SeedData.cs:16-40):**
```csharp
using Microsoft.AspNetCore.Identity;

var passwordHasher = new PasswordHasher<User>();

// Hash password when creating user
user.Password = passwordHasher.HashPassword(user, plainTextPassword);

// Verify password when logging in
var result = passwordHasher.VerifyHashedPassword(user, user.Password, plainTextPassword);
if (result == PasswordVerificationResult.Success)
{
    // Password is correct
}
```

1. **User Model** (`src/OrderPaymentSimulation.Api/Models/User.cs:1-14`)
   - Properties: Id, Name, Email, Password, CreatedAt, UpdatedAt
   - Navigation: ICollection<Order> Orders
   - Password hashing already implemented in SeedData using `PasswordHasher<User>` from Microsoft.AspNetCore.Identity

2. **User Database Configuration** (`src/OrderPaymentSimulation.Api/Data/Configurations/UserConfiguration.cs:1-48`)
   - Table: `users` (snake_case)
   - Columns: id, name, email, password, created_at, updated_at
   - Constraints: email unique index (idx_users_email), name max 100, email max 100
   - Default timestamps: CURRENT_TIMESTAMP
   - Cascade delete to Orders

3. **Database Context** (`src/OrderPaymentSimulation.Api/Data/OrderPaymentDbContext.cs`)
   - Already configured with PostgreSQL via Npgsql
   - Uses IEntityTypeConfiguration pattern
   - Connection string in appsettings.json

4. **Seed Data** (`src/OrderPaymentSimulation.Api/Data/SeedData.cs:1-110`)
   - 2 test users with hashed passwords:
     - admin@example.com / Password123!
     - test@example.com / Password123!
   - Password hashing pattern at line 16-40 shows how to hash/verify

**Controller Pattern:**
See existing controller for reference:
- `src/OrderPaymentSimulation.Api/Controllers/WeatherForecastController.cs:1-32`
  - Uses `[ApiController]` and `[Route("[controller]")]` attributes
  - Inherits from ControllerBase
  - Namespace: `order_payment_simulation_api.Controllers`

**Middleware & Configuration** (`src/OrderPaymentSimulation.Api/Program.cs:1-52`):
- DbContext registered at line 12-13
- Controllers added at line 15
- Swagger configured at line 17-18
- Authorization middleware exists at line 48 (but not configured yet)
- Database initialization at line 29-30

**Database Connection Details:**
- Host: localhost:5432
- Database: order_payment_simulation
- User: orderuser
- Password: dev_password
- Docker container: order-payment-db
- Compose file: `Postgres/docker-compose.yml`

**Template Code to Remove:**
- `src/OrderPaymentSimulation.Api/Controllers/WeatherForecastController.cs:1-32`
- `src/OrderPaymentSimulation.Api/WeatherForecast.cs`

## EXAMPLES:

**User Model (Already Exists):**
Located at `src/OrderPaymentSimulation.Api/Models/User.cs:1-14`:
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

**UserDto to Create:**
Create new folder `src/OrderPaymentSimulation.Api/Dtos/` and add UserDto:
```csharp
namespace order_payment_simulation_api.Dtos;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Static mapping method (manual mapping, no AutoMapper)
    public static UserDto CreateFrom(User user)
        => new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
}
```

**Note:** UserDto should NOT include Password field for security reasons (only used in request DTOs, not response)

**Additional DTOs to Create:**
```csharp
// For user creation (PUT api/user)
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

// For user update (POST api/user)
public class UpdateUserRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; } // Optional for update
}

// For login (POST api/auth/login)
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}
```

**Controller Pattern (based on WeatherForecastController.cs:1-32):**
Create `src/OrderPaymentSimulation.Api/Controllers/UserController.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using order_payment_simulation_api.Dtos;

namespace order_payment_simulation_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly OrderPaymentDbContext _context;
    private readonly ILogger<UserController> _logger;

    public UserController(OrderPaymentDbContext context, ILogger<UserController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPut]
    // [AllowAnonymous] - Allow user creation without authentication
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        // Validate, hash password, save to DB
        // Return 201 Created with UserDto
    }

    [HttpPost]
    [Authorize] // Requires JWT token
    public async Task<IActionResult> Update([FromBody] UpdateUserRequest request)
    {
        // Validate JWT, check authorization, update user
        // Return 200 OK with updated UserDto
    }

    // ... GET and DELETE endpoints
}
```

**Password Hashing Pattern (from SeedData.cs:16-40):**
```csharp
using Microsoft.AspNetCore.Identity;

var passwordHasher = new PasswordHasher<User>();

// Hash password when creating user
user.Password = passwordHasher.HashPassword(user, plainTextPassword);

// Verify password when logging in
var result = passwordHasher.VerifyHashedPassword(user, user.Password, plainTextPassword);
if (result == PasswordVerificationResult.Success)
{
    // Password is correct
}
>>>>>>> Stashed changes
```

## DOCUMENTATION:

**External Resources:**
- JWT Authentication in .NET: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/
- xUnit: https://xunit.net/
- AutoFixture: https://www.nuget.org/packages/autofixture
- Moq: https://www.nuget.org/packages/moq/

**Internal References:**
- Current project documentation: `CLAUDE.md:1-274`
- User model: `src/OrderPaymentSimulation.Api/Models/User.cs:1-14`
- User configuration: `src/OrderPaymentSimulation.Api/Data/Configurations/UserConfiguration.cs:1-48`
- Database context: `src/OrderPaymentSimulation.Api/Data/OrderPaymentDbContext.cs`
- Seed data (password hashing): `src/OrderPaymentSimulation.Api/Data/SeedData.cs:16-40`
- Program.cs (middleware): `src/OrderPaymentSimulation.Api/Program.cs:1-52`
- Database setup: `Postgres/docker-compose.yml`

**Database Verification:**
Use MCP server for PostgreSQL or connect directly:
```bash
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation
\dt  # List tables
SELECT * FROM users;
```

**NuGet Packages Required:**
Add to `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api.csproj`:
- Microsoft.AspNetCore.Authentication.JwtBearer (for JWT authentication)
- Microsoft.IdentityModel.Tokens (for JWT token generation)
- System.IdentityModel.Tokens.Jwt (for JWT handling)
- For tests: xunit, xunit.runner.visualstudio, AutoFixture, Moq

**ActionResult Pattern:**
Use `ActionResult<T>` from Microsoft.AspNetCore.Mvc for wrapping responses:
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<UserDto>> Get(int id)
{
    var user = await _context.Users.FindAsync(id);
    if (user == null)
        return NotFound();

    return Ok(UserDto.CreateFrom(user));
}
```

**JWT Configuration:**
Add to `appsettings.json` (similar to connection string pattern):
```json
{
  "Jwt": {
    "Key": "your-256-bit-secret-key-here-minimum-32-characters",
    "Issuer": "OrderPaymentSimulation",
    "Audience": "OrderPaymentSimulation",
    "ExpiryMinutes": 60
  }
}
```

Register in `Program.cs` (after line 13, before AddControllers):
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* configure */ });
```

**Authorization:**
- Login endpoint (`POST api/auth/login`): [AllowAnonymous]
- Create user (`PUT api/user`): [AllowAnonymous]
- All other endpoints: [Authorize] attribute

**Swagger Configuration:**
Update swagger config in `Program.cs:17-18` to support JWT bearer tokens for testing

**Testing Strategy:**
1. **Integration Tests** (`test/IntegrationTests/`)
   - Test against real PostgreSQL database (use test container or separate test DB)
   - Test full API endpoints (User CRUD, Login)
   - Verify database state changes

2. **Unit Tests** (`test/UnitTests/`)
   - Test password hashing/verification
   - Test DTO mapping
   - Test validation logic

**Database Change Verification:**
After implementation, verify using:
1. Swagger UI to call endpoints
2. PostgreSQL MCP server to query tables
3. Integration tests that assert DB state
