# PRP: User CRUD API & JWT Authentication

## Feature Overview

Implement complete user management and authentication system for the Order Payment Simulation API:
- **UserController** with full CRUD operations (Create, Read, Update, Delete)
- **AuthController** with JWT-based login functionality
- **JWT Authentication** middleware with Bearer token validation
- **Integration Tests** using xUnit, WebApplicationFactory, and AutoFixture
- **Unit Tests** for business logic validation
- **Remove** legacy WeatherForecast template code
- **Update** documentation (README.md, CLAUDE.md)

## Current Codebase Context

### Existing Infrastructure ✅

**User Model Already Exists:**
- Location: `src/OrderPaymentSimulation.Api/Models/User.cs:1-14`
- Properties: Id, Name, Email, Password, CreatedAt, UpdatedAt
- Navigation: ICollection<Order> Orders

**Database Configuration Already Complete:**
- Table: `users` (snake_case naming convention)
- Configuration: `src/OrderPaymentSimulation.Api/Data/Configurations/UserConfiguration.cs:1-48`
- Constraints:
  - Email unique index (`idx_users_email`)
  - Name max length: 100
  - Email max length: 100
  - Password hashed (required field)
  - Default timestamps: CURRENT_TIMESTAMP

**Password Hashing Already Implemented:**
- Package: `Microsoft.AspNetCore.Identity` (already in use)
- Pattern: `src/OrderPaymentSimulation.Api/Data/SeedData.cs:16-40`
- Uses: `PasswordHasher<User>` for bcrypt-based hashing

**Test Users Available:**
- admin@example.com / Password123!
- test@example.com / Password123!

**DbContext Configuration:**
- Location: `src/OrderPaymentSimulation.Api/Data/OrderPaymentDbContext.cs`
- Already registered in DI: `src/OrderPaymentSimulation.Api/Program.cs:12-13`
- Connection string: appsettings.json (PostgreSQL localhost:5432)

**Controller Pattern Reference:**
- Existing: `src/OrderPaymentSimulation.Api/Controllers/WeatherForecastController.cs:1-32`
- Uses: `[ApiController]`, `[Route("[controller]")]`, inherits `ControllerBase`
- Namespace: `order_payment_simulation_api.Controllers`

**Current NuGet Packages:**
```xml
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.6.2"/>
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4"/>
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0"/>
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0"/>
```

### Project Structure
```
src/OrderPaymentSimulation.Api/
├── Controllers/
│   └── WeatherForecastController.cs  ❌ TO REMOVE
├── Data/
│   ├── Configurations/
│   │   └── UserConfiguration.cs ✅ EXISTS
│   ├── OrderPaymentDbContext.cs ✅ EXISTS
│   └── SeedData.cs ✅ EXISTS
├── Models/
│   └── User.cs ✅ EXISTS
├── Dtos/                          ❌ TO CREATE
├── Services/                      ❌ TO CREATE (for JWT)
├── Program.cs ✅ EXISTS (needs updates)
└── appsettings.json ✅ EXISTS (needs JWT config)
```

## Implementation Approach

### High-Level Pseudocode

```
1. SETUP PHASE
   ├─ Add NuGet packages (JWT, testing)
   ├─ Create Dtos/ folder with all DTOs
   ├─ Create Services/ folder with JwtService
   └─ Add JWT configuration to appsettings.json

2. JWT SERVICE
   ├─ Create IJwtService interface
   ├─ Implement JwtService with GenerateToken method
   └─ Register service in DI container

3. CONFIGURE AUTHENTICATION
   ├─ Add authentication middleware in Program.cs
   ├─ Configure JwtBearer validation parameters
   ├─ Add UseAuthentication() before UseAuthorization()
   └─ Update Swagger to support Bearer tokens

4. IMPLEMENT CONTROLLERS
   ├─ AuthController
   │   └─ POST /api/auth/login (verify password, return JWT + UserDto)
   ├─ UserController
   │   ├─ PUT /api/user (create user, hash password) [AllowAnonymous]
   │   ├─ POST /api/user (update user, check ownership) [Authorize]
   │   ├─ GET /api/user/{id} (read user) [Authorize]
   │   └─ DELETE /api/user/{id} (delete user, check ownership) [Authorize]

5. CREATE TESTS
   ├─ Create test/ folder structure
   ├─ Integration Tests (WebApplicationFactory)
   │   ├─ AuthController tests (login scenarios)
   │   ├─ UserController tests (CRUD with JWT)
   │   └─ Database state verification
   └─ Unit Tests
       ├─ Password hashing/verification
       ├─ DTO mapping
       └─ JWT token generation/validation

6. CLEANUP & DOCUMENTATION
   ├─ Remove WeatherForecastController.cs
   ├─ Remove WeatherForecast.cs
   ├─ Update README.md (authentication section, API endpoints)
   ├─ Update CLAUDE.md (current state, new features)
   └─ Verify database changes via PostgreSQL

7. VALIDATION
   ├─ dotnet build (success)
   ├─ dotnet test (all pass)
   ├─ Manual Swagger testing (create user, login, CRUD operations)
   └─ Database verification (psql or MCP server)
```

## Detailed Implementation Tasks

### Task 1: Add Required NuGet Packages

**File:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api.csproj`

Add the following PackageReferences:
```xml
<!-- JWT Authentication -->
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.11" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.2" />

<!-- Testing will be added to test projects separately -->
```

**Command:**
```bash
cd src/OrderPaymentSimulation.Api/
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.11
dotnet add package System.IdentityModel.Tokens.Jwt --version 8.0.2
```

---

### Task 2: Create DTOs

**Create:** `src/OrderPaymentSimulation.Api/Dtos/` folder

**Files to create:**

#### `Dtos/UserDto.cs`
```csharp
namespace order_payment_simulation_api.Dtos;

/// <summary>
/// DTO for User response (excludes password for security)
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

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

#### `Dtos/CreateUserRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace order_payment_simulation_api.Dtos;

public class CreateUserRequest
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    public string Password { get; set; } = string.Empty;
}
```

#### `Dtos/UpdateUserRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace order_payment_simulation_api.Dtos;

public class UpdateUserRequest
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional: Only provide if changing password
    /// </summary>
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be between 6 and 100 characters")]
    public string? Password { get; set; }
}
```

#### `Dtos/LoginRequest.cs`
```csharp
using System.ComponentModel.DataAnnotations;

namespace order_payment_simulation_api.Dtos;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}
```

#### `Dtos/LoginResponse.cs`
```csharp
namespace order_payment_simulation_api.Dtos;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = null!;
}
```

---

### Task 3: Create JWT Service

**Create:** `src/OrderPaymentSimulation.Api/Services/` folder

#### `Services/IJwtService.cs`
```csharp
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Services;

public interface IJwtService
{
    string GenerateToken(User user);
}
```

#### `Services/JwtService.cs`
```csharp
using Microsoft.IdentityModel.Tokens;
using order_payment_simulation_api.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace order_payment_simulation_api.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]
            ?? throw new InvalidOperationException("JWT Key not configured"));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(
                double.Parse(jwtSettings["ExpiryMinutes"] ?? "60")),
            Issuer = jwtSettings["Issuer"],
            Audience = jwtSettings["Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
```

---

### Task 4: Add JWT Configuration

**Update:** `src/OrderPaymentSimulation.Api/appsettings.json`

Add JWT section (after ConnectionStrings):
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=order_payment_simulation;Username=orderuser;Password=dev_password"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm",
    "Issuer": "OrderPaymentSimulation",
    "Audience": "OrderPaymentSimulation",
    "ExpiryMinutes": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**IMPORTANT:** Also create `appsettings.Development.json` with same structure (if not exists)

**Security Note:** In production, use environment variables or secret managers for the JWT Key.

---

### Task 5: Configure Authentication in Program.cs

**Update:** `src/OrderPaymentSimulation.Api/Program.cs`

**Changes needed:**

1. **Add using statements** (at top):
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using order_payment_simulation_api.Services;
using Microsoft.OpenApi.Models;
```

2. **Register JWT Service** (after DbContext registration, before AddControllers):
```csharp
// Add JWT Service
builder.Services.AddScoped<IJwtService, JwtService>();
```

3. **Configure Authentication** (after AddControllers, before AddEndpointsApiExplorer):
```csharp
// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]
    ?? throw new InvalidOperationException("JWT Key not configured"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ClockSkew = TimeSpan.Zero // Remove default 5 minute tolerance
    };
});
```

4. **Update Swagger Configuration** (replace existing AddSwaggerGen):
```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Order Payment Simulation API",
        Version = "v1",
        Description = "ASP.NET Core 8.0 Web API with JWT Authentication"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

5. **Add Authentication Middleware** (BEFORE app.UseAuthorization()):
```csharp
app.UseAuthentication(); // Add this line
app.UseAuthorization();
```

**Complete Program.cs structure after changes:**
```
1. Using statements
2. var builder = WebApplication.CreateBuilder(args)
3. DbContext registration
4. JWT Service registration
5. AddControllers
6. Authentication configuration
7. AddEndpointsApiExplorer
8. AddSwaggerGen (with JWT support)
9. var app = builder.Build()
10. Database seeding
11. if Development: UseSwagger, UseSwaggerUI
12. UseHttpsRedirection
13. UseAuthentication ← NEW
14. UseAuthorization
15. MapControllers
16. app.Run()
```

---

### Task 6: Create AuthController

**Create:** `src/OrderPaymentSimulation.Api/Controllers/AuthController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;
using order_payment_simulation_api.Services;

namespace order_payment_simulation_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly OrderPaymentDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        OrderPaymentDbContext context,
        IJwtService jwtService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _passwordHasher = new PasswordHasher<User>();
        _logger = logger;
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Verify password
            var verificationResult = _passwordHasher.VerifyHashedPassword(
                user,
                user.Password,
                request.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);

            _logger.LogInformation("User {Email} logged in successfully", user.Email);

            return Ok(new LoginResponse
            {
                Token = token,
                User = UserDto.CreateFrom(user)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }
}
```

---

### Task 7: Create UserController

**Create:** `src/OrderPaymentSimulation.Api/Controllers/UserController.cs`

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;
using System.Security.Claims;

namespace order_payment_simulation_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly OrderPaymentDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;
    private readonly ILogger<UserController> _logger;

    public UserController(
        OrderPaymentDbContext context,
        ILogger<UserController> logger)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
        _logger = logger;
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
    /// Create a new user
    /// </summary>
    [HttpPut]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDto>> Create([FromBody] CreateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Check if email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest(new { message = "Email already exists" });
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Hash password
            user.Password = _passwordHasher.HashPassword(user, request.Password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created: {Email}", user.Email);

            return CreatedAtAction(
                nameof(Get),
                new { id = user.Id },
                UserDto.CreateFrom(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "An error occurred while creating the user" });
        }
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDto>> Update([FromBody] UpdateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var currentUserId = GetCurrentUserId();

            // Users can only update their own data
            if (currentUserId != request.Id)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to update user {TargetUserId}",
                    currentUserId, request.Id);
                return Unauthorized(new { message = "You can only update your own profile" });
            }

            var user = await _context.Users.FindAsync(request.Id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Check if new email already exists (and it's not the same user)
            if (user.Email != request.Email)
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email == request.Email && u.Id != request.Id);

                if (emailExists)
                {
                    return BadRequest(new { message = "Email already exists" });
                }
            }

            // Update fields
            user.Name = request.Name;
            user.Email = request.Email;
            user.UpdatedAt = DateTime.UtcNow;

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.Password = _passwordHasher.HashPassword(user, request.Password);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("User updated: {UserId}", user.Id);

            return Ok(UserDto.CreateFrom(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", request.Id);
            return StatusCode(500, new { message = "An error occurred while updating the user" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserDto>> Get(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(UserDto.CreateFrom(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the user" });
        }
    }

    /// <summary>
    /// Delete user (users can only delete their own account)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // Users can only delete their own account
            if (currentUserId != id)
            {
                _logger.LogWarning("User {CurrentUserId} attempted to delete user {TargetUserId}",
                    currentUserId, id);
                return Unauthorized(new { message = "You can only delete your own account" });
            }

            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User deleted: {UserId}", id);

            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the user" });
        }
    }
}
```

---

### Task 8: Create Test Projects

#### 8.1 Create Integration Tests Project

**Command:**
```bash
# From repository root
mkdir -p test/IntegrationTests
cd test/IntegrationTests
dotnet new xunit -n IntegrationTests
```

**Add packages:**
```bash
dotnet add package Microsoft.AspNetCore.Mvc.Testing --version 8.0.0
dotnet add package AutoFixture --version 4.18.1
dotnet add package Moq --version 4.20.72
dotnet add package FluentAssertions --version 6.12.2
dotnet add reference ../../src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api.csproj
```

**Create:** `test/IntegrationTests/CustomWebApplicationFactory.cs`
```csharp
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using order_payment_simulation_api.Data;

namespace IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<OrderPaymentDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<OrderPaymentDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Build service provider and create database
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<OrderPaymentDbContext>();

            db.Database.EnsureCreated();
        });
    }
}
```

**Create:** `test/IntegrationTests/AuthControllerTests.cs`
```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;
using Microsoft.AspNetCore.Identity;

namespace IntegrationTests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenAndUser()
    {
        // Arrange
        await SeedTestUser();
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        loginResponse.Should().NotBeNull();
        loginResponse!.Token.Should().NotBeNullOrEmpty();
        loginResponse.User.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        await SeedTestUser();
        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "SomePassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task SeedTestUser()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderPaymentDbContext>();

        if (!context.Users.Any(u => u.Email == "test@example.com"))
        {
            var passwordHasher = new PasswordHasher<User>();
            var user = new User
            {
                Name = "Test User",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user.Password = passwordHasher.HashPassword(user, "TestPassword123!");

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
    }
}
```

**Create:** `test/IntegrationTests/UserControllerTests.cs`
```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;
using Microsoft.AspNetCore.Identity;

namespace IntegrationTests;

public class UserControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UserControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUser_WithValidData_ReturnsCreated()
    {
        // Arrange
        var createRequest = new CreateUserRequest
        {
            Name = "New User",
            Email = "newuser@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/user", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Email.Should().Be("newuser@example.com");
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        await SeedTestUser();
        var createRequest = new CreateUserRequest
        {
            Name = "Duplicate User",
            Email = "test@example.com", // Already exists
            Password = "Password123!"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/user", createRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUser_WithAuthentication_ReturnsUser()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync($"/api/user/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto.Should().NotBeNull();
        userDto!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/user/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_ReturnsOk()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var updateRequest = new UpdateUserRequest
        {
            Id = userId,
            Name = "Updated Name",
            Email = "updated@example.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/user", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.Content.ReadFromJsonAsync<UserDto>();
        userDto!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task DeleteUser_WithAuthentication_ReturnsOk()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.DeleteAsync($"/api/user/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify user is deleted
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderPaymentDbContext>();
        var user = await context.Users.FindAsync(userId);
        user.Should().BeNull();
    }

    private async Task SeedTestUser()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderPaymentDbContext>();

        if (!context.Users.Any(u => u.Email == "test@example.com"))
        {
            var passwordHasher = new PasswordHasher<User>();
            var user = new User
            {
                Name = "Test User",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user.Password = passwordHasher.HashPassword(user, "TestPassword123!");

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
    }

    private async Task<(int userId, string token)> CreateAndLoginUser()
    {
        var email = $"user{Guid.NewGuid()}@example.com";

        // Create user
        var createRequest = new CreateUserRequest
        {
            Name = "Test User",
            Email = email,
            Password = "Password123!"
        };
        var createResponse = await _client.PutAsJsonAsync("/api/user", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Login to get token
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

**Update:** `test/IntegrationTests/IntegrationTests.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.1" />
    <PackageReference Include="xunit" Version="2.9.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
    <PackageReference Include="AutoFixture" Version="4.18.1" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="FluentAssertions" Version="6.12.2" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\OrderPaymentSimulation.Api\OrderPaymentSimulation.Api.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

</Project>
```

**Make Program.cs testable:**
Add to end of `src/OrderPaymentSimulation.Api/Program.cs`:
```csharp
// Make the implicit Program class public for testing
public partial class Program { }
```

#### 8.2 Create Unit Tests Project

**Command:**
```bash
# From repository root
cd test
mkdir UnitTests
cd UnitTests
dotnet new xunit -n UnitTests
```

**Add packages:**
```bash
dotnet add package AutoFixture --version 4.18.1
dotnet add package Moq --version 4.20.72
dotnet add package FluentAssertions --version 6.12.2
dotnet add reference ../../src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api.csproj
```

**Create:** `test/UnitTests/JwtServiceTests.cs`
```csharp
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using order_payment_simulation_api.Models;
using order_payment_simulation_api.Services;
using System.IdentityModel.Tokens.Jwt;

namespace UnitTests;

public class JwtServiceTests
{
    private readonly Fixture _fixture;
    private readonly IConfiguration _configuration;

    public JwtServiceTests()
    {
        _fixture = new Fixture();

        // Setup configuration
        var configData = new Dictionary<string, string>
        {
            { "Jwt:Key", "TestSecretKeyThatIsAtLeast32CharactersLongForHS256" },
            { "Jwt:Issuer", "TestIssuer" },
            { "Jwt:Audience", "TestAudience" },
            { "Jwt:ExpiryMinutes", "60" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    [Fact]
    public void GenerateToken_WithValidUser_ReturnsToken()
    {
        // Arrange
        var service = new JwtService(_configuration);
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = service.GenerateToken(user);

        // Assert
        token.Should().NotBeNullOrEmpty();

        // Verify token structure
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        jwtToken.Claims.Should().Contain(c => c.Type == "email" && c.Value == user.Email);
        jwtToken.Claims.Should().Contain(c => c.Type == "sub" && c.Value == user.Id.ToString());
    }

    [Fact]
    public void GenerateToken_ContainsCorrectClaims()
    {
        // Arrange
        var service = new JwtService(_configuration);
        var user = new User
        {
            Id = 42,
            Name = "John Doe",
            Email = "john@example.com",
            Password = "hashedpassword"
        };

        // Act
        var token = service.GenerateToken(user);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        var claims = jwtToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == "nameid" && c.Value == "42");
        claims.Should().Contain(c => c.Type == "name" && c.Value == "John Doe");
        claims.Should().Contain(c => c.Type == "email" && c.Value == "john@example.com");
    }
}
```

**Create:** `test/UnitTests/PasswordHashingTests.cs`
```csharp
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using order_payment_simulation_api.Models;

namespace UnitTests;

public class PasswordHashingTests
{
    [Fact]
    public void HashPassword_ProducesHashedString()
    {
        // Arrange
        var hasher = new PasswordHasher<User>();
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com" };
        var plainPassword = "MyPassword123!";

        // Act
        var hashedPassword = hasher.HashPassword(user, plainPassword);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(plainPassword);
    }

    [Fact]
    public void VerifyHashedPassword_WithCorrectPassword_Succeeds()
    {
        // Arrange
        var hasher = new PasswordHasher<User>();
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com" };
        var plainPassword = "MyPassword123!";
        var hashedPassword = hasher.HashPassword(user, plainPassword);

        // Act
        var result = hasher.VerifyHashedPassword(user, hashedPassword, plainPassword);

        // Assert
        result.Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void VerifyHashedPassword_WithWrongPassword_Fails()
    {
        // Arrange
        var hasher = new PasswordHasher<User>();
        var user = new User { Id = 1, Name = "Test", Email = "test@test.com" };
        var plainPassword = "MyPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hashedPassword = hasher.HashPassword(user, plainPassword);

        // Act
        var result = hasher.VerifyHashedPassword(user, hashedPassword, wrongPassword);

        // Assert
        result.Should().Be(PasswordVerificationResult.Failed);
    }
}
```

**Create:** `test/UnitTests/DtoMappingTests.cs`
```csharp
using FluentAssertions;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;

namespace UnitTests;

public class DtoMappingTests
{
    [Fact]
    public void UserDto_CreateFrom_MapsCorrectly()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var dto = UserDto.CreateFrom(user);

        // Assert
        dto.Id.Should().Be(user.Id);
        dto.Name.Should().Be(user.Name);
        dto.Email.Should().Be(user.Email);
        dto.CreatedAt.Should().Be(user.CreatedAt);
        dto.UpdatedAt.Should().Be(user.UpdatedAt);
    }

    [Fact]
    public void UserDto_DoesNotIncludePassword()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Password = "hashedpassword",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var dto = UserDto.CreateFrom(user);

        // Assert
        var dtoType = typeof(UserDto);
        dtoType.GetProperty("Password").Should().BeNull();
    }
}
```

---

### Task 9: Remove Template Code

**Delete files:**
1. `src/OrderPaymentSimulation.Api/Controllers/WeatherForecastController.cs`
2. `src/OrderPaymentSimulation.Api/WeatherForecast.cs`

**Command:**
```bash
cd src/OrderPaymentSimulation.Api
rm Controllers/WeatherForecastController.cs
rm WeatherForecast.cs
```

---

### Task 10: Update Documentation

#### 10.1 Update README.md

**Update:** `README.md` - Add new section after "Getting Started"

```markdown
## API Endpoints

### Authentication

#### Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "test@example.com",
  "password": "Password123!"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "name": "Test User",
    "email": "test@example.com",
    "createdAt": "2025-01-15T10:00:00Z",
    "updatedAt": "2025-01-15T10:00:00Z"
  }
}
```

### User Management

All endpoints except **Create User** require JWT Bearer token in the Authorization header:
```
Authorization: Bearer <your-jwt-token>
```

#### Create User
```http
PUT /api/user
Content-Type: application/json

{
  "name": "New User",
  "email": "newuser@example.com",
  "password": "SecurePassword123!"
}
```

**Response (201 Created):**
```json
{
  "id": 3,
  "name": "New User",
  "email": "newuser@example.com",
  "createdAt": "2025-01-15T10:00:00Z",
  "updatedAt": "2025-01-15T10:00:00Z"
}
```

#### Update User
```http
POST /api/user
Authorization: Bearer <token>
Content-Type: application/json

{
  "id": 3,
  "name": "Updated Name",
  "email": "updated@example.com",
  "password": "NewPassword123!"  // Optional
}
```

**Response (200 OK):** Updated UserDto

#### Get User
```http
GET /api/user/{id}
Authorization: Bearer <token>
```

**Response (200 OK):** UserDto

#### Delete User
```http
DELETE /api/user/{id}
Authorization: Bearer <token>
```

**Response (200 OK):**
```json
{
  "message": "User deleted successfully"
}
```

### Testing the API

1. **Using Swagger UI:**
   - Navigate to https://localhost:7006/swagger
   - Click "Authorize" button (top right)
   - Create a user using `PUT /api/user`
   - Login using `POST /api/auth/login` to get token
   - Enter token in format: `Bearer <your-token-here>`
   - Click "Authorize"
   - Now you can test protected endpoints

2. **Using curl:**
```bash
# Login
curl -X POST https://localhost:7006/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Password123!"}'

# Get user (replace {token} and {id})
curl https://localhost:7006/api/user/1 \
  -H "Authorization: Bearer {token}"
```

## Running Tests

```bash
# From repository root
dotnet test

# Run only integration tests
dotnet test test/IntegrationTests/IntegrationTests.csproj

# Run only unit tests
dotnet test test/UnitTests/UnitTests.csproj

# Run with detailed output
dotnet test --verbosity detailed
```
```

#### 10.2 Update CLAUDE.md

**Update** the "Current State" section in `CLAUDE.md`:

Replace **Implemented** section with:
```markdown
**Implemented:**
- Complete PostgreSQL database setup with Docker
- Entity Framework Core with 4 domain models (User, Product, Order, OrderItem)
- Fluent API entity configurations
- Database seeding with test data
- Password hashing using ASP.NET Identity PasswordHasher
- **JWT Authentication** with Bearer token validation
- **User CRUD API** (Create, Read, Update, Delete)
- **Authentication API** (Login with JWT)
- Authorization middleware (users can only modify their own data)
- **Integration tests** (xUnit, WebApplicationFactory, FluentAssertions)
- **Unit tests** (password hashing, JWT generation, DTO mapping)
- Proper entity relationships with cascade/restrict behaviors
- Database indexes on frequently queried fields
- Snake_case database naming convention
- Swagger UI with JWT Bearer token support
```

Replace **Pending Implementation** section with:
```markdown
**Pending Implementation:**
- Product, Order, OrderItem API controllers
- Service layer / Repository pattern (optional)
- Proper EF Core migrations (replace EnsureCreated)
- Role-based authorization (Admin, User roles)
- Refresh token mechanism
- Rate limiting
- API versioning
```

Update **Known Limitations** section:
```markdown
**Known Limitations:**
- Using `EnsureCreated()` instead of migrations (not production-ready)
- No refresh token mechanism (JWT expires after 60 minutes)
- No rate limiting implemented
- JWT secret key in appsettings.json (should use environment variables in production)
- No email verification for new users
- No password reset functionality
```

Add new section **Authentication & Authorization**:
```markdown
## Authentication & Authorization

### JWT Configuration
- Token expiry: 60 minutes (configurable in appsettings.json)
- Algorithm: HMACSHA256
- Claims: NameIdentifier, Name, Email, Sub, Jti

### Authorization Rules
- **Public endpoints:**
  - `POST /api/auth/login` - Login
  - `PUT /api/user` - Create user

- **Protected endpoints** (require JWT Bearer token):
  - `GET /api/user/{id}` - Get user
  - `POST /api/user` - Update user (can only update own profile)
  - `DELETE /api/user/{id}` - Delete user (can only delete own account)

### Testing Authentication
See README.md for Swagger UI and curl examples
```

---

## External Resources & Documentation

### Official Microsoft Documentation
- [JWT Bearer Authentication Configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0) - Official guide for configuring JWT in ASP.NET Core
- [Integration Tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) - WebApplicationFactory and TestServer documentation
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/) - General authentication overview

### Community Guides & Tutorials
- [JWT Authentication in .NET 8: A Complete Guide](https://medium.com/@solomongetachew112/jwt-authentication-in-net-8-a-complete-guide-for-secure-and-scalable-applications-6281e5e8667c) - Comprehensive 2025 guide with examples
- [.NET 8.0 Web API JWT Authentication](https://dev.to/shahed1bd/net-80-web-api-jwt-authentication-and-role-based-authorization-42f1) - Practical implementation guide
- [Integration Testing for ASP.NET APIs](https://knowyourtoolset.com/2024/01/integration-testing/) - Best practices for integration testing
- [ASP.NET Core Integration Testing Best Practices](https://antondevtips.com/blog/asp-net-core-integration-testing-best-practises) - Testing patterns and strategies

### NuGet Packages Documentation
- [Microsoft.AspNetCore.Authentication.JwtBearer](https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.JwtBearer) - Version 8.0.11
- [System.IdentityModel.Tokens.Jwt](https://www.nuget.org/packages/System.IdentityModel.Tokens.Jwt) - Version 8.0.2
- [Microsoft.AspNetCore.Mvc.Testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing) - Version 8.0.0
- [xUnit](https://xunit.net/) - Testing framework
- [AutoFixture](https://www.nuget.org/packages/autofixture) - Test data generation
- [Moq](https://www.nuget.org/packages/moq/) - Mocking framework
- [FluentAssertions](https://www.nuget.org/packages/FluentAssertions) - Assertion library

## Common Pitfalls & Gotchas

### 1. JWT Secret Key Length ⚠️
**Issue:** HS256 requires minimum 256-bit (32 characters) secret key
**Solution:** Ensure JWT:Key in appsettings.json is at least 32 characters
```json
"Key": "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm"
```

### 2. Authentication Middleware Order ⚠️
**Issue:** `UseAuthentication()` must come BEFORE `UseAuthorization()`
**Correct order in Program.cs:**
```csharp
app.UseHttpsRedirection();
app.UseAuthentication();  // FIRST
app.UseAuthorization();   // SECOND
app.MapControllers();
```

### 3. Program Class Visibility for Tests ⚠️
**Issue:** Integration tests need access to Program class
**Solution:** Add to end of Program.cs:
```csharp
public partial class Program { }
```

### 4. DbContext in Integration Tests ⚠️
**Issue:** Tests hitting real database can cause conflicts
**Solution:** Use InMemoryDatabase in CustomWebApplicationFactory

### 5. Password Hashing Context ⚠️
**Issue:** PasswordHasher requires User instance even for verification
**Correct usage:**
```csharp
var hasher = new PasswordHasher<User>();
var result = hasher.VerifyHashedPassword(user, user.Password, plainTextPassword);
```

### 6. ModelState Validation ⚠️
**Issue:** Data annotations don't automatically return 400 BadRequest
**Solution:** Check ModelState in controller:
```csharp
if (!ModelState.IsValid)
{
    return BadRequest(ModelState);
}
```

### 7. ClockSkew in JWT ⚠️
**Issue:** Default 5-minute tolerance can allow expired tokens
**Solution:** Set `ClockSkew = TimeSpan.Zero` in TokenValidationParameters

### 8. Entity Framework Tracking ⚠️
**Issue:** EF Core tracks entities, causing update conflicts
**Note:** Using `FindAsync()` automatically tracks entities for updates

### 9. Async/Await Pattern ⚠️
**Issue:** Missing `await` causes tasks to run synchronously
**Always use:** `await _context.SaveChangesAsync();`

### 10. CORS for Production ⚠️
**Issue:** Frontend apps need CORS configuration
**Note:** Not implemented in current PRP, add if needed for SPA frontends

## Validation Gates

### Build Validation
```bash
# From repository root
dotnet build OrderPaymentSimulation.Api.sln
```
**Expected:** Build succeeds with 0 errors

### Test Validation
```bash
# Run all tests
dotnet test

# Run integration tests only
dotnet test test/IntegrationTests/IntegrationTests.csproj

# Run unit tests only
dotnet test test/UnitTests/UnitTests.csproj
```
**Expected:** All tests pass

### Manual API Validation

1. **Start the API:**
```bash
cd src/OrderPaymentSimulation.Api
dotnet run
```

2. **Access Swagger:** https://localhost:7006/swagger

3. **Test Flow:**
   - ✅ Create user (PUT /api/user) → 201 Created
   - ✅ Login (POST /api/auth/login) → 200 OK with token
   - ✅ Copy token, click "Authorize" in Swagger, paste token
   - ✅ Get user (GET /api/user/{id}) → 200 OK
   - ✅ Update user (POST /api/user) → 200 OK
   - ✅ Delete user (DELETE /api/user/{id}) → 200 OK
   - ✅ Try accessing protected endpoint without token → 401 Unauthorized
   - ✅ Try updating different user's data → 401 Unauthorized

### Database Validation

**Option 1: PostgreSQL CLI**
```bash
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation
```
```sql
SELECT * FROM users;
-- Verify:
-- 1. Created users exist
-- 2. Passwords are hashed (not plain text)
-- 3. Updated data is reflected
-- 4. Deleted users are removed
```

**Option 2: MCP Server**
Use MCP PostgreSQL server to query `users` table and verify data changes

## Success Criteria

- [ ] ✅ All NuGet packages installed successfully
- [ ] ✅ DTOs created with proper validation attributes
- [ ] ✅ JwtService generates valid JWT tokens
- [ ] ✅ JWT configuration in appsettings.json
- [ ] ✅ Authentication middleware configured in Program.cs
- [ ] ✅ Swagger supports Bearer token authentication
- [ ] ✅ AuthController implements login with password verification
- [ ] ✅ UserController implements CRUD operations
- [ ] ✅ Authorization checks prevent users from modifying others' data
- [ ] ✅ Integration tests project created and passing
- [ ] ✅ Unit tests project created and passing
- [ ] ✅ WeatherForecast template code removed
- [ ] ✅ README.md updated with API documentation
- [ ] ✅ CLAUDE.md updated with current state
- [ ] ✅ `dotnet build` succeeds
- [ ] ✅ `dotnet test` all tests pass
- [ ] ✅ Manual Swagger testing successful
- [ ] ✅ Database changes verified in PostgreSQL

## Task Execution Order

Execute tasks in this exact order for optimal results:

1. ✅ Add NuGet packages (Task 1)
2. ✅ Create all DTOs (Task 2)
3. ✅ Create JWT Service (Task 3)
4. ✅ Add JWT configuration to appsettings.json (Task 4)
5. ✅ Update Program.cs with authentication (Task 5)
6. ✅ Create AuthController (Task 6)
7. ✅ Create UserController (Task 7)
8. ✅ Build and verify compilation
9. ✅ Create Integration Tests project (Task 8.1)
10. ✅ Create Unit Tests project (Task 8.2)
11. ✅ Run tests and fix any issues
12. ✅ Remove WeatherForecast template code (Task 9)
13. ✅ Update README.md (Task 10.1)
14. ✅ Update CLAUDE.md (Task 10.2)
15. ✅ Final validation (build, test, manual testing, database verification)

## PRP Quality Score

**Confidence Level: 9/10**

**Strengths:**
- ✅ Complete codebase context with file paths and line numbers
- ✅ Existing User model and database configuration already in place
- ✅ Password hashing pattern already implemented in SeedData
- ✅ Comprehensive JWT implementation guide with .NET 8 specifics
- ✅ Detailed integration testing setup with WebApplicationFactory
- ✅ All code examples are complete and executable
- ✅ Common pitfalls documented with solutions
- ✅ External resources from official Microsoft docs and recent 2024-2025 guides
- ✅ Clear task execution order
- ✅ Executable validation gates

**Minor Risks (why not 10/10):**
- ⚠️ Test database seeding might have race conditions (mitigated by InMemoryDatabase)
- ⚠️ JWT secret key needs to be updated before production deployment
- ⚠️ First-time integration test setup might need minor adjustments

**Mitigation:**
- All risks are well-documented in "Common Pitfalls" section
- Test examples use InMemoryDatabase to avoid conflicts
- Security warnings clearly marked for production considerations

---

## Implementation Notes

- The User model already exists with proper database configuration
- Password hashing is already implemented in SeedData.cs
- The codebase follows snake_case for database naming
- All controllers use attribute routing pattern
- Entity Framework Core 8.0 is already configured
- PostgreSQL database is running in Docker
- Seeded test users are available for testing

This PRP provides all context needed for autonomous, one-pass implementation with high confidence of success.
