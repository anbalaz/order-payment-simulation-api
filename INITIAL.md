## FEATURE:

Initialize Kafka in docker

I have docker desktop app. Run Kafka in docker and Update existing docker-compose file so this service can be created inside docker. Include kafka into compose file, stored in Folder Postgres.
Add mcp server and connect to it, add this settings to repository, so you can use it.

move existing ./Postgres/docker-compose.yml file to root of project.

add Kafka into the project, so the messages/events can be sent/published as transport system.

---------------------------------------------
## CODEBASE CONTEXT:

### Existing Docker Setup
- **Current location:** `./Postgres/docker-compose.yml` (lines 1-24)
- **Current services:** PostgreSQL 16-alpine container named `order-payment-db`
- **Existing volumes:** `postgres_data` with local driver
- **Target:** Move this file to project root and add Kafka service alongside PostgreSQL

### Existing Order Controller
- **File:** `src/OrderPaymentSimulation.Api/Controllers/OrderController.cs`
- **Create endpoint:** Line 109-214 (`PUT /api/order`) - Creates order with status 'Pending' (line 187)
- **Current flow:** Order creation → Stock deduction → SaveChangesAsync (line 194) → Logger confirmation (line 196)
- **Integration point:** After line 194 (`await _context.SaveChangesAsync()`), publish 'OrderCreated' event

### OrderStatus Enum
- **File:** `src/OrderPaymentSimulation.Api/Models/OrderStatus.cs` (lines 3-10)
- **Existing values:** Pending (0), Processing (1), Completed (2), Cancelled (3), Expired (4)
- **Note:** 'Expired' status already exists and is ready to use for the background service

### Database Configuration Pattern
- **Pattern:** IEntityTypeConfiguration<T> in `Data/Configurations/` folder
- **Example:** `OrderConfiguration.cs` (lines 1-47) shows fluent API with snake_case naming
- **New entity needed:** Notification table following same pattern (snake_case columns, proper indexes)
- **Registration:** Add configuration to `OrderPaymentDbContext.OnModelCreating()` (line 19-28)

### Database Seeding
- **File:** `src/OrderPaymentSimulation.Api/Data/SeedData.cs`
- **Current seeds:** Users (line 19-43), Products (line 46-57), Orders (line 59-89), OrderItems (line 91-107)
- **Add:** Notification seed data following same pattern

### Service Registration
- **File:** `src/OrderPaymentSimulation.Api/Program.cs`
- **DbContext registration:** Line 17-18 (uses PostgreSQL with Npgsql)
- **Service registration example:** Line 21 (IJwtService)
- **Add:** Kafka producer/consumer services, background service for order expiration

### Existing Integration Tests
- **File:** `test/IntegrationTests/IntegrationTests/OrderControllerTests.cs`
- **Test pattern:** Uses `CustomWebApplicationFactory`, FluentAssertions, JWT authentication
- **Example:** Line 24-50 shows order creation test with product stock setup
- **Framework:** xUnit with IClassFixture pattern
- **Add:** Tests for Kafka event publishing and notification creation

### Existing Unit Tests Location
- **Folder:** `test/UnitTests/UnitTests/`
- **Dependencies:** xUnit, FluentAssertions, AutoFixture, Moq
- **Add:** Unit tests for event handlers and background service logic

---------------------------------------------
## How the kafka should work:

When the order is created and stored in db with status 'pending', the 'OrderCreated' event has to be published

**Implementation point:** `OrderController.cs:194` - After `SaveChangesAsync()`, before loading created order

OrderCreatedHandler (asynchronous) should handle this event and :
    -handles 'OrderCreated' events
    -Update order status: to 'processing' (update it in db)
    -simulate payment processing (5 second delay)
    -after this 5 sec simulation Update order status for 50% of cases to 'completed' and publish 'OrderCompleted' event
    -In another 50% of cases do not change the status, it remains as 'processing'

----------------------------------------------
## Order expiration handling:

**Background Service Pattern:**
- Register as hosted service in `Program.cs` (similar to how DbContext is registered at line 17-18)
- Implement `BackgroundService` base class
- Use scoped services via `IServiceScopeFactory` for DbContext access

**Implementation:**
    - Add hosted backgroundService that runs every 60 seconds, gets all orders older than 10 minutes and have status 'processing' and updates it's status to 'expired'
    - after updating publish 'OrderExpired' event
    - **Note:** `OrderStatus.Expired` already exists in `Models/OrderStatus.cs:9` and is ready to use

----------------------------------------------
## Add new notification table into postgres db, update also seed script

**Entity Configuration Pattern (follow existing pattern):**
- Create `Models/Notification.cs` entity
- Create `Data/Configurations/NotificationConfiguration.cs` implementing `IEntityTypeConfiguration<Notification>`
- Use snake_case for table and column names (see `OrderConfiguration.cs` as reference)
- Add foreign keys to User and Order with proper relationships
- Add indexes on foreign keys (example: `OrderConfiguration.cs:30-31`)
- Register in `OrderPaymentDbContext.cs:28` via `modelBuilder.ApplyConfiguration()`
- Add `DbSet<Notification> Notifications` property to context (see line 14-17 for examples)

**Table fields:**
- reference to user (foreign key to users table)
- reference to order (foreign key to orders table)
- notification text/message field
- order completion status (completed/expired)
- standard timestamps: created_at, updated_at with `CURRENT_TIMESTAMP` default (see `OrderConfiguration.cs:33-39`)

**Seed Data:**
- Update `SeedData.Initialize()` method in `Data/SeedData.cs`
- Add sample notifications after line 107 (after OrderItems seeding)
- Follow existing pattern: check if already seeded, create entities with timestamps

Notification handler (asynchronous)
    - handles 'OrderCompleted' and 'OrderExpired' events
    - when 'OrderCompleted' event is published, log fake email notification into console (something like: order number 1234 for user user@email.com was issued containing products :...). Save notification to 'notification' table in database (as an audit trail)
    - when 'OrderExpired' event is published, save notification to database (audit trail)

-----------------------------------------------
## Expected Flow:
1. User creates order via POST /api/orders
2. Order saved to DB with status='pending'
3. OrderCreated event published
4. OrderProcessor handles event asynchronously:
  -Updates status to 'processing'
  -Simulates payment (5 sec delay)
  -Updates status to 'completed' (50% of cases)
5. OrderCompleted event published (if applicable)
6. Notifier handles event:
  -Logs fake email to console
  -Saves notification to DB
7. CRON job (background service) runs every 60s:
  -Finds processing orders older than 10 minutes
  -Updates them to 'expired'
  -Publishes OrderExpired event

-----------------------------------------------
## Testing Requirements:

**Integration Tests** (`test/IntegrationTests/IntegrationTests/`):
- Add tests to new file or extend `OrderControllerTests.cs`
- Test pattern: Use `CustomWebApplicationFactory` (see `OrderControllerTests.cs:12-21`)
- Test order creation triggers Kafka event publishing
- Test notification creation after OrderCompleted/OrderExpired events
- Use JWT authentication for protected endpoints (see `OrderControllerTests.cs:28`)
- Dependencies: xUnit, FluentAssertions, HttpClient for API testing

**Unit Tests** (`test/UnitTests/UnitTests/`):
- Create separate test files for event handlers and background service
- Test OrderCreatedHandler logic (payment simulation, status updates)
- Test NotificationHandler logic (console logging, DB insertion)
- Test background service expiration logic (query filtering, status update)
- Dependencies: xUnit, FluentAssertions, AutoFixture for test data, Moq for mocking DbContext/repositories

**Test Execution:**
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test test/IntegrationTests/IntegrationTests/IntegrationTests.csproj
dotnet test test/UnitTests/UnitTests/UnitTests.csproj
```

-----------------------------------------------
## Database Cleanup and Verification:

when finished drop database and seed it

**Commands:**
```bash
# Stop and remove PostgreSQL container (drops database)
cd Postgres
docker-compose down -v

# Start fresh PostgreSQL
docker-compose up -d

# Application will auto-create and seed database on next startup via Program.cs:97-98
```

test if the expected workflow works

**Manual Testing Checklist:**
1. Create order via `PUT /api/order` (see `OrderController.cs:109`)
2. Verify order status is 'Pending' in database
3. Check console logs for OrderCreated event
4. Wait 5 seconds, verify status changes to 'Processing' then 'Completed' or stays 'Processing'
5. Check console for fake email notification (if completed)
6. Verify notification record in notifications table
7. Wait for orders in 'Processing' status > 10 minutes
8. Verify background service updates them to 'Expired' after 60-second cycle
9. Check notifications table for expiration audit trail

-----------------------------------------------
## Documentation Updates:

add integration tests into IntegrationTests project that is in folder test in root of the project example
if necessary add unit tests into UnitTests project that is in folder test in root of the project example

For tests use x-unit tests and also autofixture (https://www.nuget.org/packages/autofixture), Moq (https://www.nuget.org/packages/moq/)

update Readme about new features.

**README.md should include:**
- Kafka setup instructions
- Docker compose commands for full stack (PostgreSQL + Kafka)
- New API behavior with event publishing
- Background service description
- Notification table schema
- Testing instructions

after everything works update Claude.md

**CLAUDE.md updates needed:**
- Add Kafka to Technology Stack section
- Document new Notification entity in Entity Models section
- Add BackgroundService to Architecture section
- Update "Current State" implemented features list
- Add event flow diagram/description
- Update testing statistics (add new test counts)
- Document new NuGet packages (Kafka client library)

## DOCUMENTATION:

to check data and format look into mcp server for postgres tables

**MCP Server for PostgreSQL:**
- Use MCP server to inspect database schema and data format
- Verify notification table structure after creation
- Check relationships between users, orders, and notifications
- Validate data types and constraints match configuration

**External References:**
- AutoFixture: https://www.nuget.org/packages/autofixture
- Moq: https://www.nuget.org/packages/moq/
- Confluent Kafka .NET: https://docs.confluent.io/kafka-clients/dotnet/current/overview.html (recommended)
- Apache Kafka Docker: https://hub.docker.com/r/confluentinc/cp-kafka/
- ASP.NET Core BackgroundService: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services

**Internal References:**
- Project overview: `CLAUDE.md` (complete project architecture and conventions)
- Database context: `src/OrderPaymentSimulation.Api/Data/OrderPaymentDbContext.cs`
- Entity configurations: `src/OrderPaymentSimulation.Api/Data/Configurations/` folder
- Existing tests: `test/IntegrationTests/IntegrationTests/` and `test/UnitTests/UnitTests/`
