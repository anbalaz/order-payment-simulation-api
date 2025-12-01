## FEATURE:

Initialize Postgres in docker

I have docker desktop app. Run Postgres in docker and initialize it with docker compose file. Include compose file in repository, store it in Folder Postgres. Database should hold data about orders, users, payments, so choose good name for it.
Add mcp server to Postgres and connect to it, add this settings to repository, so you can use it.

In postgres should be these tables:

Users:
id
name max length 100,
email max length 100 and unique,
password string (should be hashed and protected like passwords are in db, so nobody can decipher them).

Products:
id,
name max length 100,
description string,
price number >=0,
created_at timestamp

Orders:
id,
user_id,
total number >=0,
status (should be enum, in db store it as tinyInt or similar type),
items schema id (primary key),
product_id,
quantity (number>0)
price (number>0)
created_at timestamp
updated_at timestamp

In orders user_id is id from Users table and product_id is reference to id from Products table.

Include in DBS also initial seed data for tables. These scripts tore in Postgres folder.

Include into the final solution DB upgrade mechanism. It has to contain some form of upgrade
DB scripts or DB upgrade code.

Create README.md file in root of project with documentation on:
- How to start PostgreSQL database using Docker Compose
- How to run DB upgrade/migration tool
- How to start the service
- Prerequisites and dependencies
- Basic project overview

## PROJECT CONTEXT:

**Current Project Structure:**
- Solution file: `OrderPaymentSimulation.Api.sln` (root directory)
- Main API project: `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/`
- Root namespace: `order_payment_simulation_api`
- Target framework: .NET 8.0
- Current dependencies: Only Swashbuckle.AspNetCore 6.6.2 for Swagger/OpenAPI

**Relevant Files to Reference:**
- Project file: `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api.csproj:1-14`
- Application entry point: `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Program.cs:1-25`
- Configuration: `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/appsettings.json:1-9`
- Project documentation: `CLAUDE.md:1-52`

**Current State:**
- Minimal ASP.NET Core boilerplate with default WeatherForecast controller
- No database configuration or Entity Framework setup yet
- No connection strings configured in appsettings.json
- Controllers use attribute routing pattern `[Route("[controller]")]`
- Swagger enabled in Development environment
- Application runs on http://localhost:5267 and https://localhost:7006

**Implementation Notes:**
1. **NuGet Packages Required:**
   - Add `Npgsql.EntityFrameworkCore.PostgreSQL` for PostgreSQL provider
   - Add `Microsoft.EntityFrameworkCore.Design` for migrations tooling
   - Consider `Microsoft.EntityFrameworkCore.Tools` for EF Core CLI commands
   - Consider migration library (e.g., FluentMigrator or EF Core Migrations)

2. **Docker Compose File Location:**
   - Create new directory: `Postgres/` in repository root
   - Store `docker-compose.yml` in `Postgres/` folder
   - Store seed scripts (.sql files) in `Postgres/` folder

3. **Configuration Updates:**
   - Add connection string to `appsettings.json:1-9` and `appsettings.Development.json`
   - Register DbContext in `Program.cs:1-25` (add after line 5: `builder.Services.AddControllers();`)
   - Consider creating separate configuration section for database settings

4. **Project Structure to Create:**
   - `README.md` in repository root with complete setup documentation
   - `Models/` directory for entity classes (User, Product, Order, OrderItem)
   - `Data/` directory for DbContext and configurations
   - `Data/Migrations/` for EF Core migrations or migration scripts
   - `Postgres/` directory for Docker compose and seed scripts

5. **Database Schema Considerations:**
   - Use PostgreSQL conventions (snake_case for column names is common)
   - Implement proper indexing on foreign keys and email field
   - Use `timestamp with time zone` for timestamp fields in PostgreSQL
   - Consider using PostgreSQL sequences for auto-incrementing IDs
   - For password hashing, use ASP.NET Core Identity's PasswordHasher or bcrypt

6. **Database Migration Strategy Options:**
   - **Option A:** EF Core Migrations (integrates with Entity Framework, code-first approach)
   - **Option B:** FluentMigrator (more control, supports versioned migration scripts)
   - **Option C:** DbUp (simple SQL script runner with version tracking)
   - Recommendation: Start with EF Core Migrations for .NET 8.0 integration

7. **README.md Documentation:**
   - Create `README.md` in repository root
   - Document complete setup process from scratch
   - Include Docker Compose commands for database
   - Document migration/upgrade commands (e.g., `dotnet ef database update`)
   - Include service startup commands
   - List all prerequisites (Docker Desktop, .NET 8.0 SDK)
   - Add troubleshooting section for common issues

## EXAMPLES:

**Docker Compose Structure:**
Create `Postgres/docker-compose.yml`:
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
volumes:
  postgres_data:
```

**Connection String Example for appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=order_payment_simulation;Username=orderuser;Password=dev_password"
  },
  "Logging": { ... }
}
```

**DbContext Registration in Program.cs (after line 5):**
```csharp
builder.Services.AddDbContext<OrderPaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**README.md Structure Example:**
Create `README.md` in repository root:
```markdown
# Order Payment Simulation API

ASP.NET Core 8.0 Web API for simulating order payment workflows.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- Git

## Getting Started

### 1. Start PostgreSQL Database

Navigate to the Postgres directory and start the database:

\`\`\`bash
cd Postgres
docker-compose up -d
\`\`\`

Verify the database is running:
\`\`\`bash
docker ps
\`\`\`

### 2. Run Database Migrations

Navigate to the API project directory and run migrations:

\`\`\`bash
cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
dotnet ef database update
\`\`\`

This will create all tables and apply seed data.

### 3. Start the API Service

From the API project directory:

\`\`\`bash
dotnet run
\`\`\`

Or run with specific profile:
\`\`\`bash
dotnet run --launch-profile https  # https://localhost:7006
dotnet run --launch-profile http   # http://localhost:5267
\`\`\`

### 4. Access Swagger UI

Open your browser and navigate to:
- https://localhost:7006/swagger (HTTPS)
- http://localhost:5267/swagger (HTTP)

## Database Information

- **Database Name:** order_payment_simulation
- **Port:** 5432
- **Username:** orderuser
- **Password:** dev_password (development only)

## Available Commands

### Build the solution
\`\`\`bash
dotnet build OrderPaymentSimulation.Api.sln
\`\`\`

### Run tests
\`\`\`bash
dotnet test
\`\`\`

### Create new migration
\`\`\`bash
cd src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api
dotnet ef migrations add MigrationName
\`\`\`

### Stop the database
\`\`\`bash
cd Postgres
docker-compose down
\`\`\`

## Troubleshooting

### Database connection fails
- Ensure Docker Desktop is running
- Check if PostgreSQL container is running: `docker ps`
- Verify port 5432 is not in use by another application

### Migration errors
- Ensure database is running before running migrations
- Check connection string in appsettings.json
- Try: `dotnet ef database drop` then `dotnet ef database update`
```

## DOCUMENTATION:

**External Resources:**
- PostgreSQL with .NET: https://www.npgsql.org/efcore/
- EF Core Migrations: https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/
- Docker Compose: https://docs.docker.com/compose/
- ASP.NET Core Data Access: https://learn.microsoft.com/en-us/aspnet/core/data/

**Internal References:**
- Project overview and build commands: `CLAUDE.md:1-52`
- Current project configuration: `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api.csproj:1-14`
- Application startup and middleware: `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api/Program.cs:1-25`
- Setup documentation (to be created): `README.md` in repository root

**MCP Server Setup:**
- MCP (Model Context Protocol) server configuration should be added to repository
- Typically stored in `.claude/` directory or similar configuration location
- Will require PostgreSQL connection details matching docker-compose configuration
