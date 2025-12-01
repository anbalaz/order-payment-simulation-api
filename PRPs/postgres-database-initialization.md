# PRP: PostgreSQL Database Initialization

**Feature:** Initialize PostgreSQL in Docker with EF Core migrations, seed data, and MCP server configuration

**Created:** 2025-11-30

**Status:** Ready for Implementation

---

## 1. FEATURE OVERVIEW

Initialize a PostgreSQL database using Docker Compose for the Order Payment Simulation API. The implementation includes:

- PostgreSQL 16 running in Docker with persistent storage
- Entity Framework Core 8.0 with Npgsql provider
- Database schema with Users, Products, Orders, and OrderItems tables
- EF Core migrations for schema management
- Seed data for initial development/testing
- MCP (Model Context Protocol) server configuration for AI-assisted database access
- Comprehensive README.md documentation

**Business Value:**
- Enables order and payment workflow simulation
- Provides repeatable database setup across development environments
- Establishes foundation for future API endpoints

---

## 2. CURRENT STATE ANALYSIS

### Existing Codebase Structure

```
C:\Mine\order-payment-simulation-api\
├── OrderPaymentSimulation.Api.sln
├── CLAUDE.md (project documentation)
├── src\
│   └── OrderPaymentSimulation.Api\
│       └── OrderPaymentSimulation.Api\
│           ├── OrderPaymentSimulation.Api.csproj
│           ├── Program.cs (lines 1-25)
│           ├── appsettings.json (lines 1-9)
│           ├── appsettings.Development.json (lines 1-8)
│           ├── Controllers\
│           │   └── WeatherForecastController.cs
│           └── Properties\
│               └── launchSettings.json
└── .claude\
    └── settings.local.json
```

### Current Configuration

**Project File:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api.csproj`
- Target Framework: net8.0
- Root Namespace: order_payment_simulation_api
- Dependencies: Swashbuckle.AspNetCore 6.6.2

**Program.cs:** Standard ASP.NET Core boilerplate with:
- Controllers registered (line 5)
- Swagger/OpenAPI configured (lines 7-8, 14-16)
- HTTPS redirection enabled (line 19)
- Authorization middleware (line 21)

**appsettings.json:** Minimal configuration with logging only

**Launch Profiles:**
- HTTP: http://localhost:5267
- HTTPS: https://localhost:7006;http://localhost:5267

### What's Missing
- No database packages (Npgsql, EF Core)
- No connection strings
- No entity models
- No DbContext
- No Docker configuration
- No MCP server setup
- No README.md

---

## 3. TECHNICAL RESEARCH & DOCUMENTATION

### 3.1 NuGet Packages (Latest for .NET 8.0)

**Required Packages:**
1. **Npgsql.EntityFrameworkCore.PostgreSQL** (v8.0.4)
   - Official EF Core provider for PostgreSQL
   - Documentation: https://www.npgsql.org/efcore/
   - Release Notes: https://www.npgsql.org/efcore/release-notes/8.0.html

2. **Microsoft.EntityFrameworkCore.Design** (v8.0.x)
   - Required for EF Core tooling and migrations
   - Documentation: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/

3. **Microsoft.EntityFrameworkCore.Tools** (v8.0.x)
   - Enables `dotnet ef` CLI commands
   - Documentation: https://learn.microsoft.com/en-us/ef/core/cli/dotnet

### 3.2 Key Technologies & Best Practices

#### Entity Framework Core with PostgreSQL
- **Official Documentation:** https://www.npgsql.org/efcore/
- **Migrations Guide:** https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- **GitHub Repository:** https://github.com/npgsql/efcore.pg

**Best Practices (2025):**
- Use `ToJson` for JSON column mapping (EF Core 8.0 feature)
- Set PostgreSQL version explicitly if using older versions: `SetPostgresVersion(14, 0)`
- Use primitive collections for arrays with full LINQ support
- Provider automatically adjusts sequences when seeding data with identity columns

#### Data Seeding
- **Official Documentation:** https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding
- **Modern Approach:** https://juliocasal.com/blog/how-to-seed-data-with-ef-core-9-and-net-aspire

**Best Practices:**
1. **Use `UseSeeding/UseAsyncSeeding`** (Recommended for EF Core 8+)
   - Protected by migration locking mechanism
   - Prevents concurrency issues
   - Better for large datasets
   - Supports custom transformations (password hashing)
   - Data NOT captured in migration snapshots

2. **Use `HasData`** (For static reference data)
   - Good for small, unchanging data (ZIP codes, etc.)
   - Data captured in migration snapshots
   - Generated values must be specified manually

**For this project:** Use `UseSeeding` for Users/Products/Orders seed data with password hashing

#### Docker Compose for PostgreSQL
- **Docker Documentation:** https://docs.docker.com/compose/
- **Best Practices:** https://earthly.dev/blog/postgres-docker/
- **Volume Guide:** https://dev.to/iamrj846/how-to-persist-data-in-a-dockerized-postgres-database-using-volumes-15f0

**Best Practices:**
- Use **named volumes** for persistence (not bind mounts in production)
- Mount at `/var/lib/postgresql/data` (not parent directory)
- Use alpine images for smaller size (`postgres:16-alpine`)
- Named volumes survive `docker-compose down`
- For development: consider bind mounts for initialization scripts

**Volume Configuration:**
```yaml
volumes:
  postgres_data:  # Named volume (Docker-managed)
```

#### MCP Server for PostgreSQL
- **Official MCP Server:** https://mcp.so/server/postgres/modelcontextprotocol
- **Setup Guide:** https://rowanblackwoon.medium.com/how-to-setup-and-use-postgresql-mcp-server-82fc3915e5c1
- **NPM Package:** https://www.npmjs.com/package/@modelcontextprotocol/server-postgres

**Configuration Format:**
```json
{
  "mcpServers": {
    "postgres": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-postgres",
        "postgresql://localhost/database_name"
      ]
    }
  }
}
```

**Security:** MCP PostgreSQL server enforces read-only transactions by default

---

## 4. DATABASE SCHEMA DESIGN

### 4.1 Tables and Relationships

```
Users (1) ----< Orders (N)
                  |
                  | contains
                  v
              OrderItems (N) >---- (1) Products
```

### 4.2 Table Specifications

#### Users Table
```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    email VARCHAR(100) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,  -- bcrypt or PBKDF2 hash
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_users_email ON users(email);
```

**C# Entity:**
- Id: int (auto-generated)
- Name: string (max 100, required)
- Email: string (max 100, required, unique)
- PasswordHash: string (required, never exposed in API)
- CreatedAt: DateTime (auto-set)
- UpdatedAt: DateTime (auto-set)

#### Products Table
```sql
CREATE TABLE products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    description TEXT,
    price DECIMAL(10,2) NOT NULL CHECK (price >= 0),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
```

**C# Entity:**
- Id: int (auto-generated)
- Name: string (max 100, required)
- Description: string (nullable)
- Price: decimal (>= 0, precision 10,2)
- CreatedAt: DateTime (auto-set)

#### Orders Table
```sql
CREATE TABLE orders (
    id SERIAL PRIMARY KEY,
    user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    total DECIMAL(10,2) NOT NULL CHECK (total >= 0),
    status SMALLINT NOT NULL,  -- Enum: Pending=0, Processing=1, Completed=2, Cancelled=3
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_orders_user_id ON orders(user_id);
CREATE INDEX idx_orders_status ON orders(status);
```

**C# Entity:**
- Id: int (auto-generated)
- UserId: int (required, FK to Users)
- Total: decimal (>= 0, precision 10,2)
- Status: OrderStatus enum (stored as smallint)
- CreatedAt: DateTime (auto-set)
- UpdatedAt: DateTime (auto-set)
- User: Navigation property to User
- OrderItems: Collection navigation property

**OrderStatus Enum:**
```csharp
public enum OrderStatus : short
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Cancelled = 3
}
```

#### OrderItems Table
```sql
CREATE TABLE order_items (
    id SERIAL PRIMARY KEY,
    order_id INTEGER NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
    product_id INTEGER NOT NULL REFERENCES products(id) ON DELETE RESTRICT,
    quantity INTEGER NOT NULL CHECK (quantity > 0),
    price DECIMAL(10,2) NOT NULL CHECK (price > 0),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_order_items_order_id ON order_items(order_id);
CREATE INDEX idx_order_items_product_id ON order_items(product_id);
```

**C# Entity:**
- Id: int (auto-generated)
- OrderId: int (required, FK to Orders)
- ProductId: int (required, FK to Products)
- Quantity: int (> 0, required)
- Price: decimal (> 0, precision 10,2) -- Snapshot of product price at order time
- CreatedAt: DateTime (auto-set)
- Order: Navigation property to Order
- Product: Navigation property to Product

### 4.3 PostgreSQL Naming Conventions

**Convention:** Use `snake_case` for table and column names in PostgreSQL
**EF Core Mapping:** Use `ToSnakeCase()` or explicit column name attributes

---

## 5. IMPLEMENTATION APPROACH (PSEUDOCODE)

### Phase 1: Project Setup
```
1. Create directory structure:
   - src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Models/
   - src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/
   - src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/Configurations/
   - Postgres/
   - Postgres/init-scripts/

2. Add NuGet packages to .csproj:
   - Npgsql.EntityFrameworkCore.PostgreSQL (8.0.4)
   - Microsoft.EntityFrameworkCore.Design (8.0.x)
   - Microsoft.EntityFrameworkCore.Tools (8.0.x)
```

### Phase 2: Entity Models
```
3. Create Models/OrderStatus.cs:
   - Define enum with Pending, Processing, Completed, Cancelled

4. Create Models/User.cs:
   - Properties: Id, Name, Email, PasswordHash, CreatedAt, UpdatedAt
   - Navigation: Orders collection

5. Create Models/Product.cs:
   - Properties: Id, Name, Description, Price, CreatedAt
   - Navigation: OrderItems collection (optional)

6. Create Models/Order.cs:
   - Properties: Id, UserId, Total, Status, CreatedAt, UpdatedAt
   - Navigation: User, OrderItems collection

7. Create Models/OrderItem.cs:
   - Properties: Id, OrderId, ProductId, Quantity, Price, CreatedAt
   - Navigation: Order, Product
```

### Phase 3: Data Layer
```
8. Create Data/OrderPaymentDbContext.cs:
   - Inherit from DbContext
   - Define DbSet<User>, DbSet<Product>, DbSet<Order>, DbSet<OrderItem>
   - Override OnModelCreating():
     - Apply entity configurations
     - Configure snake_case naming
   - Override OnConfiguring() if needed for PostgreSQL-specific features

9. Create Data/Configurations/UserConfiguration.cs (IEntityTypeConfiguration<User>):
   - Table name: "users"
   - Primary key: Id
   - Properties: Name (max 100, required), Email (max 100, required, unique index)
   - PasswordHash (required)
   - Timestamps with default values

10. Create Data/Configurations/ProductConfiguration.cs:
    - Table name: "products"
    - Properties: Name (max 100), Price (precision 10,2, >= 0)

11. Create Data/Configurations/OrderConfiguration.cs:
    - Table name: "orders"
    - Foreign key: UserId -> Users
    - Status: Convert enum to smallint
    - Indexes on UserId and Status

12. Create Data/Configurations/OrderItemConfiguration.cs:
    - Table name: "order_items"
    - Foreign keys: OrderId -> Orders, ProductId -> Products
    - Check constraints on Quantity (>0) and Price (>0)
```

### Phase 4: Configuration & Registration
```
13. Update appsettings.json:
    - Add ConnectionStrings section:
      "DefaultConnection": "Host=localhost;Port=5432;Database=order_payment_simulation;Username=orderuser;Password=dev_password"

14. Update appsettings.Development.json:
    - Same connection string (can override for dev environment)

15. Update Program.cs (after line 5: builder.Services.AddControllers()):
    - Add DbContext registration:
      builder.Services.AddDbContext<OrderPaymentDbContext>(options =>
          options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    - Add seeding logic BEFORE app.Build():
      builder.Services.AddDbContext<OrderPaymentDbContext>((sp, options) =>
      {
          options.UseNpgsql(connectionString);

          // Configure seeding
          options.UseSeeding((context, _) =>
          {
              SeedData.Initialize(context);
          });
      });
```

### Phase 5: Docker Configuration
```
16. Create Postgres/docker-compose.yml:
    version: '3.8'
    services:
      postgres:
        image: postgres:16-alpine
        container_name: order-payment-db
        environment:
          POSTGRES_DB: order_payment_simulation
          POSTGRES_USER: orderuser
          POSTGRES_PASSWORD: dev_password
        ports:
          - "5432:5432"
        volumes:
          - postgres_data:/var/lib/postgresql/data
          - ./init-scripts:/docker-entrypoint-initdb.d
    volumes:
      postgres_data:

17. Create Postgres/.env (optional, for variable management):
    POSTGRES_DB=order_payment_simulation
    POSTGRES_USER=orderuser
    POSTGRES_PASSWORD=dev_password
```

### Phase 6: Database Migrations
```
18. Install EF Core tools globally:
    dotnet tool install --global dotnet-ef

19. Create initial migration from project directory:
    cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
    dotnet ef migrations add InitialCreate

20. Review generated migration files in Data/Migrations/
```

### Phase 7: Seed Data
```
21. Create Data/SeedData.cs:
    - Method: Initialize(OrderPaymentDbContext context)
    - Check if data exists (avoid duplicate seeding)
    - Use ASP.NET Core Identity PasswordHasher for password hashing:
      var hasher = new PasswordHasher<User>();
      user.PasswordHash = hasher.HashPassword(user, "Password123!");

    - Seed Users:
      * Admin user: admin@example.com
      * Test user: test@example.com

    - Seed Products:
      * 5-10 sample products with realistic prices

    - Seed Orders:
      * 3-5 sample orders for test user
      * Mix of statuses

    - Seed OrderItems:
      * Line items for each order
      * Calculate order totals

    - Save changes: context.SaveChanges()

22. Alternative: Create Postgres/init-scripts/seed.sql (if using SQL seeding):
    - INSERT statements for sample data
    - Note: UseSeeding is preferred for password hashing
```

### Phase 8: MCP Server Configuration
```
23. Create .claude/mcp.json (or update existing):
    {
      "mcpServers": {
        "postgres": {
          "command": "npx",
          "args": [
            "-y",
            "@modelcontextprotocol/server-postgres",
            "postgresql://orderuser:dev_password@localhost:5432/order_payment_simulation"
          ]
        }
      }
    }

24. Test MCP connection (after starting Docker and running migrations)
```

### Phase 9: Documentation
```
25. Create README.md in repository root:
    # Order Payment Simulation API

    ## Prerequisites
    - .NET 8.0 SDK
    - Docker Desktop
    - Git

    ## Getting Started

    ### 1. Start PostgreSQL Database
    cd Postgres
    docker-compose up -d
    docker ps  # Verify running

    ### 2. Run Database Migrations
    cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
    dotnet ef database update

    ### 3. Start the API Service
    dotnet run --launch-profile https

    ### 4. Access Swagger UI
    https://localhost:7006/swagger

    ## Database Information
    - Database: order_payment_simulation
    - Port: 5432
    - User: orderuser
    - Password: dev_password (dev only)

    ## Available Commands
    - Build: dotnet build OrderPaymentSimulation.Api.sln
    - Test: dotnet test
    - New migration: dotnet ef migrations add <Name>
    - Update database: dotnet ef database update
    - Stop database: cd Postgres && docker-compose down

    ## Troubleshooting
    [Common issues and solutions]
```

### Phase 10: Validation
```
26. Start Docker PostgreSQL:
    cd Postgres && docker-compose up -d

27. Verify database connection:
    docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation

28. Apply migrations:
    cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
    dotnet ef database update

29. Verify schema:
    - Check tables created: \dt in psql
    - Check seed data: SELECT * FROM users;

30. Build and run application:
    dotnet build
    dotnet run

31. Test API:
    - Open https://localhost:7006/swagger
    - Verify Swagger UI loads
```

---

## 6. DETAILED IMPLEMENTATION TASKS (In Order)

### Task 1: Create Directory Structure
**Action:** Create necessary directories for models, data layer, and Docker configuration

**Files to Create:**
- `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Models/` (directory)
- `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/` (directory)
- `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/Configurations/` (directory)
- `Postgres/` (directory in repo root)
- `Postgres/init-scripts/` (directory)

**Validation:** Verify directories exist using file explorer or `dir` command

---

### Task 2: Add NuGet Packages
**Action:** Add required Entity Framework Core packages to the project

**File to Modify:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api.csproj`

**Packages to Add:**
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0" />
```

**Command:**
```bash
cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.4
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet restore
```

**Validation:** `dotnet list package` should show the new packages

---

### Task 3: Create OrderStatus Enum
**Action:** Define the order status enumeration

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Models/OrderStatus.cs`

**Content:**
```csharp
namespace order_payment_simulation_api.Models;

public enum OrderStatus : short
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Cancelled = 3
}
```

**Validation:** File compiles without errors

---

### Task 4: Create User Entity
**Action:** Define User entity model

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Models/User.cs`

**Content:**
```csharp
namespace order_payment_simulation_api.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
```

**Validation:** File compiles without errors

---

### Task 5: Create Product Entity
**Action:** Define Product entity model

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Models/Product.cs`

**Content:**
```csharp
namespace order_payment_simulation_api.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

**Validation:** File compiles without errors

---

### Task 6: Create Order Entity
**Action:** Define Order entity model

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Models/Order.cs`

**Content:**
```csharp
namespace order_payment_simulation_api.Models;

public class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
```

**Validation:** File compiles without errors

---

### Task 7: Create OrderItem Entity
**Action:** Define OrderItem entity model

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Models/OrderItem.cs`

**Content:**
```csharp
namespace order_payment_simulation_api.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
```

**Validation:** File compiles without errors

---

### Task 8: Create DbContext
**Action:** Create main database context class

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/OrderPaymentDbContext.cs`

**Content:**
```csharp
using Microsoft.EntityFrameworkCore;
using order_payment_simulation_api.Models;
using order_payment_simulation_api.Data.Configurations;

namespace order_payment_simulation_api.Data;

public class OrderPaymentDbContext : DbContext
{
    public OrderPaymentDbContext(DbContextOptions<OrderPaymentDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());
    }
}
```

**Validation:** File compiles (may have errors until configurations are created)

---

### Task 9: Create User Configuration
**Action:** Configure User entity mappings

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/Configurations/UserConfiguration.cs`

**Content:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("idx_users_email");

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasMany(u => u.Orders)
            .WithOne(o => o.User)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**Validation:** DbContext now compiles without errors

---

### Task 10: Create Product Configuration
**Action:** Configure Product entity mappings

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/Configurations/ProductConfiguration.cs`

**Content:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(p => p.Price)
            .HasColumnName("price")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships
        builder.HasMany(p => p.OrderItems)
            .WithOne(oi => oi.Product)
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

**Validation:** Compiles without errors

---

### Task 11: Create Order Configuration
**Action:** Configure Order entity mappings

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/Configurations/OrderConfiguration.cs`

**Content:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property(o => o.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(o => o.Total)
            .HasColumnName("total")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();

        builder.HasIndex(o => o.Status)
            .HasDatabaseName("idx_orders_status");

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(o => o.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Relationships configured in UserConfiguration and OrderItemConfiguration
        builder.HasMany(o => o.OrderItems)
            .WithOne(oi => oi.Order)
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

**Validation:** Compiles without errors

---

### Task 12: Create OrderItem Configuration
**Action:** Configure OrderItem entity mappings

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/Configurations/OrderItemConfiguration.cs`

**Content:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(oi => oi.Id);
        builder.Property(oi => oi.Id).HasColumnName("id");

        builder.Property(oi => oi.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.HasIndex(oi => oi.OrderId)
            .HasDatabaseName("idx_order_items_order_id");

        builder.Property(oi => oi.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.HasIndex(oi => oi.ProductId)
            .HasDatabaseName("idx_order_items_product_id");

        builder.Property(oi => oi.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(oi => oi.Price)
            .HasColumnName("price")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(oi => oi.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
}
```

**Validation:** Compiles without errors

---

### Task 13: Update appsettings.json
**Action:** Add database connection string

**File to Modify:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/appsettings.json`

**Changes:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=order_payment_simulation;Username=orderuser;Password=dev_password"
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

**Validation:** Valid JSON syntax

---

### Task 14: Update appsettings.Development.json
**Action:** Add development-specific connection string (same as above for now)

**File to Modify:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/appsettings.Development.json`

**Changes:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=order_payment_simulation;Username=orderuser;Password=dev_password"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

**Validation:** Valid JSON syntax

---

### Task 15: Create SeedData Helper
**Action:** Create seeding logic with password hashing

**File to Create:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/SeedData.cs`

**Content:**
```csharp
using Microsoft.AspNetCore.Identity;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Data;

public static class SeedData
{
    public static void Initialize(OrderPaymentDbContext context)
    {
        // Check if already seeded
        if (context.Users.Any())
        {
            return; // Database has been seeded
        }

        var passwordHasher = new PasswordHasher<User>();

        // Seed Users
        var users = new[]
        {
            new User
            {
                Name = "Admin User",
                Email = "admin@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new User
            {
                Name = "Test User",
                Email = "test@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        // Hash passwords
        foreach (var user in users)
        {
            user.PasswordHash = passwordHasher.HashPassword(user, "Password123!");
        }

        context.Users.AddRange(users);
        context.SaveChanges();

        // Seed Products
        var products = new[]
        {
            new Product { Name = "Laptop", Description = "High-performance laptop", Price = 999.99m, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Mouse", Description = "Wireless mouse", Price = 29.99m, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Monitor", Description = "27-inch 4K monitor", Price = 399.99m, CreatedAt = DateTime.UtcNow },
            new Product { Name = "Headphones", Description = "Noise-cancelling headphones", Price = 199.99m, CreatedAt = DateTime.UtcNow }
        };

        context.Products.AddRange(products);
        context.SaveChanges();

        // Seed Orders
        var testUser = users[1]; // Test user
        var order1 = new Order
        {
            UserId = testUser.Id,
            Status = OrderStatus.Completed,
            Total = 1029.98m,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-4)
        };

        var order2 = new Order
        {
            UserId = testUser.Id,
            Status = OrderStatus.Processing,
            Total = 479.98m,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var order3 = new Order
        {
            UserId = testUser.Id,
            Status = OrderStatus.Pending,
            Total = 199.99m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Orders.AddRange(order1, order2, order3);
        context.SaveChanges();

        // Seed Order Items
        var orderItems = new[]
        {
            // Order 1 items
            new OrderItem { OrderId = order1.Id, ProductId = products[0].Id, Quantity = 1, Price = 999.99m, CreatedAt = DateTime.UtcNow.AddDays(-5) },
            new OrderItem { OrderId = order1.Id, ProductId = products[1].Id, Quantity = 1, Price = 29.99m, CreatedAt = DateTime.UtcNow.AddDays(-5) },

            // Order 2 items
            new OrderItem { OrderId = order2.Id, ProductId = products[3].Id, Quantity = 1, Price = 399.99m, CreatedAt = DateTime.UtcNow.AddDays(-2) },
            new OrderItem { OrderId = order2.Id, ProductId = products[2].Id, Quantity = 1, Price = 79.99m, CreatedAt = DateTime.UtcNow.AddDays(-2) },

            // Order 3 items
            new OrderItem { OrderId = order3.Id, ProductId = products[4].Id, Quantity = 1, Price = 199.99m, CreatedAt = DateTime.UtcNow }
        };

        context.OrderItems.AddRange(orderItems);
        context.SaveChanges();
    }
}
```

**Validation:** Compiles without errors

---

### Task 16: Update Program.cs
**Action:** Register DbContext and configure seeding

**File to Modify:** `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Program.cs`

**Changes:** Add after line 4 (before `builder.Services.AddControllers();`):

```csharp
using order_payment_simulation_api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<OrderPaymentDbContext>(options =>
{
    options.UseNpgsql(connectionString);

    // Configure seeding
    options.UseSeeding((context, _) =>
    {
        if (context is OrderPaymentDbContext dbContext)
        {
            SeedData.Initialize(dbContext);
        }
    });

    options.UseAsyncSeeding(async (context, _, cancellationToken) =>
    {
        if (context is OrderPaymentDbContext dbContext)
        {
            await Task.Run(() => SeedData.Initialize(dbContext), cancellationToken);
        }
    });
});

builder.Services.AddControllers();
// ... rest of existing code
```

**Note:** EF Core 8.0 might not have UseSeeding/UseAsyncSeeding. Alternative approach using middleware:

```csharp
// After app.Build() and before app.Run():
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<OrderPaymentDbContext>();
        context.Database.Migrate(); // Apply migrations
        SeedData.Initialize(context); // Seed data
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}
```

**Validation:** Compiles without errors

---

### Task 17: Create Docker Compose File
**Action:** Configure PostgreSQL container

**File to Create:** `Postgres/docker-compose.yml`

**Content:**
```yaml
version: '3.8'

services:
  postgres:
    image: postgres:16-alpine
    container_name: order-payment-db
    environment:
      POSTGRES_DB: order_payment_simulation
      POSTGRES_USER: orderuser
      POSTGRES_PASSWORD: dev_password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - ./init-scripts:/docker-entrypoint-initdb.d
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U orderuser -d order_payment_simulation"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  postgres_data:
    driver: local
```

**Validation:** Valid YAML syntax

---

### Task 18: Create init-scripts Directory Readme
**Action:** Document the init-scripts directory purpose

**File to Create:** `Postgres/init-scripts/README.md`

**Content:**
```markdown
# Database Initialization Scripts

This directory is mounted to `/docker-entrypoint-initdb.d` in the PostgreSQL container.

SQL scripts (`.sql`) and shell scripts (`.sh`) placed here will be executed automatically when the container is first created, in alphabetical order.

## Current Setup

The application uses Entity Framework Core migrations for schema management and seeding, so this directory is currently empty.

## Alternative Seeding

If you prefer SQL-based seeding instead of EF Core:

1. Create `01-seed.sql` with INSERT statements
2. Restart the container: `docker-compose down -v && docker-compose up -d`

Note: Files here only run on initial database creation, not on every container start.
```

**Validation:** File created

---

### Task 19: Install EF Core Tools
**Action:** Install dotnet-ef CLI tool globally

**Command:**
```bash
dotnet tool install --global dotnet-ef
# Or update if already installed:
dotnet tool update --global dotnet-ef
```

**Validation:**
```bash
dotnet ef --version
# Should show version 8.0.x
```

---

### Task 20: Create Initial Migration
**Action:** Generate EF Core migration for initial schema

**Commands:**
```bash
cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
dotnet ef migrations add InitialCreate
```

**Expected Output:**
- Creates `Data/Migrations/` directory
- Generates migration files: `<timestamp>_InitialCreate.cs` and `<timestamp>_InitialCreate.Designer.cs`
- Generates `OrderPaymentDbContextModelSnapshot.cs`

**Validation:** Check migration files exist and review generated SQL

---

### Task 21: Create MCP Server Configuration
**Action:** Configure MCP server for PostgreSQL access

**File to Create or Modify:** `.claude/mcp.json`

**Content:**
```json
{
  "mcpServers": {
    "postgres": {
      "command": "npx",
      "args": [
        "-y",
        "@modelcontextprotocol/server-postgres",
        "postgresql://orderuser:dev_password@localhost:5432/order_payment_simulation"
      ]
    }
  }
}
```

**Alternative:** If settings are in `.claude/settings.local.json`, add to that file instead

**Validation:** Valid JSON syntax

---

### Task 22: Create README.md
**Action:** Comprehensive project documentation

**File to Create:** `README.md` (in repository root)

**Content:**
```markdown
# Order Payment Simulation API

ASP.NET Core 8.0 Web API for simulating order payment workflows with PostgreSQL database.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL)
- Git
- (Optional) [dotnet-ef CLI tool](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) for database migrations

## Project Structure

```
order-payment-simulation-api/
├── src/
│   └── OrderPaymentSimulation.Api/
│       └── OrderPaymentSimulation.Api/          # Main API project
│           ├── Models/                          # Entity models
│           ├── Data/                            # DbContext and configurations
│           ├── Controllers/                     # API controllers
│           └── Program.cs                       # Application entry point
├── Postgres/                                    # Docker configuration
│   ├── docker-compose.yml                       # PostgreSQL container setup
│   └── init-scripts/                            # Database init scripts
├── PRPs/                                        # Project Requirements & Plans
├── CLAUDE.md                                    # AI assistant guidance
└── README.md                                    # This file
```

## Database Schema

The application uses the following tables:

- **users** - User accounts with hashed passwords
- **products** - Product catalog
- **orders** - Customer orders
- **order_items** - Order line items (many-to-many between orders and products)

See `CLAUDE.md` for detailed schema information.

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd order-payment-simulation-api
```

### 2. Start PostgreSQL Database

Navigate to the Postgres directory and start the database using Docker Compose:

```bash
cd Postgres
docker-compose up -d
```

Verify the database is running:

```bash
docker ps
```

You should see a container named `order-payment-db` running.

### 3. Install EF Core Tools (First Time Only)

```bash
dotnet tool install --global dotnet-ef
```

Or update if already installed:

```bash
dotnet tool update --global dotnet-ef
```

### 4. Run Database Migrations

Navigate to the API project directory:

```bash
cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
```

Apply migrations to create tables:

```bash
dotnet ef database update
```

This command will:
- Create all database tables (users, products, orders, order_items)
- Apply all pending migrations
- Seed initial test data automatically

### 5. Build and Run the API

From the API project directory:

```bash
dotnet build
dotnet run
```

Or run with specific launch profile:

```bash
dotnet run --launch-profile https  # Runs on https://localhost:7006
dotnet run --launch-profile http   # Runs on http://localhost:5267
```

### 6. Access Swagger UI

Open your browser and navigate to:

- **HTTPS:** https://localhost:7006/swagger
- **HTTP:** http://localhost:5267/swagger

The Swagger UI provides interactive API documentation and testing capabilities.

## Database Information

**Default Development Configuration:**

- **Database Name:** order_payment_simulation
- **Host:** localhost
- **Port:** 5432
- **Username:** orderuser
- **Password:** dev_password (**WARNING:** For development only!)

**Connection String:**
```
Host=localhost;Port=5432;Database=order_payment_simulation;Username=orderuser;Password=dev_password
```

## Seeded Test Data

The database is automatically seeded with:

**Users:**
- admin@example.com (password: Password123!)
- test@example.com (password: Password123!)

**Products:**
- Laptop ($999.99)
- Mouse ($29.99)
- Keyboard ($79.99)
- Monitor ($399.99)
- Headphones ($199.99)

**Sample Orders:**
- 3 orders for test user with various statuses

## Available Commands

### Build the Solution

```bash
# From repository root
dotnet build OrderPaymentSimulation.Api.sln
```

### Run Tests

```bash
# From repository root (when tests are added)
dotnet test
```

### Database Management

#### Create a New Migration

```bash
cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
dotnet ef migrations add <MigrationName>
```

#### Apply Migrations

```bash
dotnet ef database update
```

#### Rollback Migration

```bash
dotnet ef database update <PreviousMigrationName>
```

#### Remove Last Migration (if not applied)

```bash
dotnet ef migrations remove
```

#### Drop Database

```bash
dotnet ef database drop
```

### Docker Commands

#### Stop the Database

```bash
cd Postgres
docker-compose down
```

#### Stop and Remove Volumes (deletes all data)

```bash
docker-compose down -v
```

#### View Logs

```bash
docker-compose logs -f postgres
```

#### Access PostgreSQL Shell

```bash
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation
```

Useful PostgreSQL commands:
```sql
\dt                          -- List all tables
\d users                     -- Describe users table
SELECT * FROM users;         -- Query users
\q                           -- Quit
```

## Troubleshooting

### Database Connection Fails

**Issue:** Cannot connect to PostgreSQL

**Solutions:**
- Ensure Docker Desktop is running
- Check if PostgreSQL container is running: `docker ps`
- Verify port 5432 is not in use by another application
- Check connection string in `appsettings.json`
- Restart Docker container: `cd Postgres && docker-compose restart`

### Migration Errors

**Issue:** `dotnet ef database update` fails

**Solutions:**
- Ensure PostgreSQL database is running before applying migrations
- Check connection string in `appsettings.json` and `appsettings.Development.json`
- Verify EF Core tools are installed: `dotnet ef --version`
- Try dropping and recreating: `dotnet ef database drop` then `dotnet ef database update`
- Check migration files in `Data/Migrations/` for syntax errors

### Port Already in Use

**Issue:** Port 5432 already in use

**Solutions:**
- Stop other PostgreSQL instances
- Change port in `docker-compose.yml`:
  ```yaml
  ports:
    - "5433:5432"  # Use port 5433 on host
  ```
- Update connection string to match new port

### Application Won't Start

**Issue:** `dotnet run` fails

**Solutions:**
- Check for compilation errors: `dotnet build`
- Ensure all NuGet packages are restored: `dotnet restore`
- Check port conflicts (5267, 7006)
- Review application logs for specific error messages

### Seed Data Not Applied

**Issue:** Database tables are empty

**Solutions:**
- Check if seeding ran: look for log messages during startup
- Manually run seeding (if implemented as separate method)
- Verify `SeedData.Initialize()` is called in `Program.cs`
- Drop and recreate database: `dotnet ef database drop && dotnet ef database update`

## Development Workflow

1. **Make changes** to entity models or add new features
2. **Create migration**: `dotnet ef migrations add <DescriptiveName>`
3. **Review migration** code in `Data/Migrations/`
4. **Apply migration**: `dotnet ef database update`
5. **Test changes** using Swagger UI or unit tests
6. **Commit** migration files to version control

## Security Notes

⚠️ **WARNING:** The default credentials are for development only!

**For Production:**
- Use strong passwords
- Store credentials in environment variables or secret management systems (Azure Key Vault, AWS Secrets Manager)
- Never commit production credentials to version control
- Use SSL/TLS for database connections
- Implement proper authentication and authorization

## Technology Stack

- **.NET 8.0** - Application framework
- **ASP.NET Core Web API** - REST API framework
- **Entity Framework Core 8.0** - ORM
- **Npgsql 8.0** - PostgreSQL data provider
- **PostgreSQL 16** - Database
- **Docker** - Containerization
- **Swagger/OpenAPI** - API documentation

## Additional Resources

- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Npgsql Documentation](https://www.npgsql.org/efcore/)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [Docker Documentation](https://docs.docker.com/)

## License

[Specify your license here]

## Contributing

[Add contribution guidelines if applicable]
```

**Validation:** File created and rendered correctly in markdown viewer

---

### Task 23: Build Solution
**Action:** Verify all code compiles

**Command:**
```bash
# From repository root
dotnet build OrderPaymentSimulation.Api.sln
```

**Expected Output:** Build succeeded with no errors

**Validation:** Exit code 0, no compilation errors

---

### Task 24: Start Docker PostgreSQL
**Action:** Launch database container

**Commands:**
```bash
cd Postgres
docker-compose up -d
docker ps
```

**Expected Output:** Container `order-payment-db` is running

**Validation:** `docker ps` shows the container with status "Up"

---

### Task 25: Apply Migrations and Seed Data
**Action:** Create database schema and populate initial data

**Commands:**
```bash
cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
dotnet ef database update
```

**Expected Output:**
- Applying migration '<timestamp>_InitialCreate'
- Done.

**Validation:** Check database has tables and data:
```bash
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation -c "\dt"
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation -c "SELECT * FROM users;"
```

---

### Task 26: Run the Application
**Action:** Start the API service

**Command:**
```bash
cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
dotnet run
```

**Expected Output:**
- Application started
- Now listening on: http://localhost:5267 and https://localhost:7006

**Validation:** Open browser to https://localhost:7006/swagger and verify Swagger UI loads

---

### Task 27: Verify MCP Server Configuration
**Action:** Test MCP server connection (optional, requires Claude Desktop or compatible client)

**Steps:**
1. Restart Claude Desktop app (if using)
2. Check MCP servers are loaded
3. Try querying database through MCP

**Validation:** MCP server appears in available servers list

---

## 7. VALIDATION GATES

These commands MUST pass for successful implementation:

### Validation Gate 1: Compilation
```bash
cd C:\Mine\order-payment-simulation-api
dotnet build OrderPaymentSimulation.Api.sln
```

**Success Criteria:** Build succeeded, 0 errors

---

### Validation Gate 2: Docker PostgreSQL Running
```bash
docker ps --filter "name=order-payment-db" --format "{{.Status}}"
```

**Success Criteria:** Output contains "Up"

---

### Validation Gate 3: Database Schema Created
```bash
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation -c "\dt"
```

**Success Criteria:** Tables `users`, `products`, `orders`, `order_items` exist

---

### Validation Gate 4: Seed Data Present
```bash
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation -c "SELECT COUNT(*) FROM users;"
```

**Success Criteria:** Count is >= 2 (admin and test users)

---

### Validation Gate 5: Application Runs
```bash
cd src\OrderPaymentSimulation.Api\OrderPaymentSimulation.Api
dotnet run --no-launch-profile &
# Wait 5 seconds
curl -k https://localhost:7006/swagger/index.html -I
```

**Success Criteria:** HTTP 200 OK response

---

### Validation Gate 6: EF Migrations Work
```bash
cd src\OrderPaymentSimulation.Api\OrderPaymentSimulation.Api
dotnet ef migrations list
```

**Success Criteria:** Shows "InitialCreate" migration

---

## 8. ERROR HANDLING & EDGE CASES

### 8.1 Potential Issues

**Issue 1: Port Conflicts**
- **Problem:** Port 5432 already in use by another PostgreSQL instance
- **Solution:** Stop other PostgreSQL or change port in docker-compose.yml to 5433
- **Prevention:** Check running services before starting Docker

**Issue 2: EF Core Tooling Missing**
- **Problem:** `dotnet ef` command not found
- **Solution:** Install with `dotnet tool install --global dotnet-ef`
- **Prevention:** Document in prerequisites

**Issue 3: Migration Already Applied**
- **Problem:** Re-running migrations that already exist
- **Solution:** Use `dotnet ef database update` (idempotent)
- **Prevention:** Check migration history before creating new migrations

**Issue 4: Seed Data Duplication**
- **Problem:** Running seed multiple times creates duplicates
- **Solution:** Check `if (context.Users.Any()) return;` in SeedData.Initialize
- **Prevention:** Implement idempotent seeding

**Issue 5: Docker Volume Permissions**
- **Problem:** Permission denied on Windows with bind mounts
- **Solution:** Use named volumes instead (already in docker-compose.yml)
- **Prevention:** Follow best practices for Docker volumes

**Issue 6: Password Hashing Performance**
- **Problem:** Slow application startup due to password hashing in seed
- **Solution:** Only seed if database is empty (already implemented)
- **Prevention:** Use conditional seeding

**Issue 7: Connection String Missing**
- **Problem:** Application can't find connection string
- **Solution:** Ensure appsettings.json has "ConnectionStrings:DefaultConnection"
- **Prevention:** Validate configuration on startup

### 8.2 Rollback Strategy

If implementation fails:

1. **Drop Database:**
   ```bash
   dotnet ef database drop --force
   ```

2. **Remove Migrations:**
   ```bash
   rm -rf src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Data/Migrations
   ```

3. **Stop Docker:**
   ```bash
   cd Postgres && docker-compose down -v
   ```

4. **Restore Clean State:**
   ```bash
   git checkout -- .
   git clean -fd
   ```

---

## 9. TESTING CHECKLIST

After implementation, verify:

- [ ] Docker PostgreSQL container starts successfully
- [ ] Database has correct tables (users, products, orders, order_items)
- [ ] Seed data populated correctly (2 users, 5 products, 3 orders)
- [ ] Passwords are properly hashed (not plaintext)
- [ ] Foreign key constraints work (try deleting user with orders - should fail)
- [ ] Indexes created (check `idx_users_email`, `idx_orders_status`, etc.)
- [ ] Application builds without errors
- [ ] Application starts and serves Swagger UI
- [ ] README.md instructions are accurate
- [ ] MCP server connects to database (if configured)
- [ ] Migrations can be applied and rolled back

---

## 10. QUALITY CHECKLIST

Evaluate this PRP:

- [x] All necessary context included (project structure, current state, requirements)
- [x] Validation gates are executable by AI (specific bash commands with expected outputs)
- [x] References existing patterns (Program.cs structure, appsettings.json format)
- [x] Clear implementation path (27 detailed tasks in order)
- [x] Error handling documented (8 common issues with solutions)
- [x] External documentation linked with URLs
- [x] Best practices incorporated (UseSeeding, named volumes, password hashing)
- [x] Code examples provided (entity configurations, docker-compose.yml)
- [x] Database schema fully specified (SQL and C# entity definitions)
- [x] Security considerations addressed (password hashing, connection string warnings)
- [x] Rollback strategy documented
- [x] Testing checklist included

---

## 11. CONFIDENCE SCORE

**Implementation Confidence: 9/10**

**Rationale:**

**Strengths:**
- Comprehensive step-by-step tasks (27 tasks)
- All required code snippets provided
- Multiple validation gates with executable commands
- Best practices from official documentation incorporated
- Error handling and rollback strategies documented
- Real-world testing checklist
- Security considerations (password hashing, not plaintext)

**Potential Challenges:**
- UseSeeding/UseAsyncSeeding might not be available in EF Core 8.0.0 (released as EF Core 9.0 feature)
  - **Mitigation:** Alternative approach provided using scope service and middleware
- First-time Docker users might need additional Docker Desktop setup guidance
  - **Mitigation:** Prerequisites section links to Docker Desktop
- Windows path separators in bash commands
  - **Mitigation:** Commands written for Windows PowerShell/cmd with backslashes

**Success Likelihood:**
With this PRP, an AI agent should be able to implement the entire feature in a single pass with 90% confidence. The 10% risk accounts for:
- Potential EF Core 8.0 API differences (UseSeeding availability)
- Docker Desktop configuration variations
- Windows-specific path or permission issues

**Recommendation:** Proceed with implementation. Monitor for EF Core 8.0 seeding API availability and use middleware fallback if needed.

---

## 12. SOURCES & REFERENCES

### Documentation
- [Npgsql Entity Framework Core Provider](https://www.npgsql.org/efcore/)
- [Npgsql 8.0 Release Notes](https://www.npgsql.org/efcore/release-notes/8.0.html)
- [EF Core Data Seeding](https://learn.microsoft.com/en-us/ef/core/modeling/data-seeding)
- [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [PostgreSQL MCP Server](https://mcp.so/server/postgres/modelcontextprotocol)
- [MCP Server Setup Guide](https://rowanblackwoon.medium.com/how-to-setup-and-use-postgresql-mcp-server-82fc3915e5c1)
- [Docker PostgreSQL Best Practices](https://earthly.dev/blog/postgres-docker/)
- [PostgreSQL Docker Volumes](https://dev.to/iamrj846/how-to-persist-data-in-a-dockerized-postgres-database-using-volumes-15f0)

### Community Resources
- [How to Seed Data with EF Core 9](https://juliocasal.com/blog/how-to-seed-data-with-ef-core-9-and-net-aspire)
- [Stack Overflow - EF Core PostgreSQL](https://stackoverflow.com/questions/tagged/npgsql)
- [GitHub - Npgsql EF Core Provider](https://github.com/npgsql/efcore.pg)

---

**End of PRP**

**Next Steps:**
1. Review this PRP for completeness
2. Execute tasks sequentially
3. Run validation gates after each major phase
4. Document any deviations or issues encountered
5. Update README.md if actual implementation differs from plan
