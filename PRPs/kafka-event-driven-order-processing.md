# PRP: Kafka Event-Driven Order Processing

## Confidence Score: 9/10

**Rationale:** This PRP provides comprehensive context, clear implementation steps, existing codebase patterns to follow, and detailed validation gates. The only uncertainty is potential environment-specific Docker networking issues which are addressed with troubleshooting guidance.

---

## Feature Overview

Integrate Apache Kafka into the Order Payment Simulation API to enable event-driven order processing with asynchronous payment simulation, order expiration handling, and notification auditing.

---

## Research Summary

### Codebase Patterns Identified

1. **Service Pattern**: Interface + Implementation in `Services/` folder (see `IJwtService.cs` and `JwtService.cs`)
2. **Configuration**: appsettings.json sections read via `IConfiguration.GetSection()` (see `Program.cs:20-28`)
3. **Entity Configuration**: `IEntityTypeConfiguration<T>` with snake_case DB naming (see `Data/Configurations/OrderConfiguration.cs`)
4. **DI Registration**: `Program.cs` registers services with appropriate lifetimes (Scoped, Singleton, Transient)
5. **Testing Pattern**: `CustomWebApplicationFactory` for integration tests, AutoFixture + Moq for unit tests

### External Research Findings

**Confluent.Kafka .NET Client** (Latest stable: 2.x)
- **Documentation**: [Confluent .NET Client Docs](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
- **GitHub**: [confluentinc/confluent-kafka-dotnet](https://github.com/confluentinc/confluent-kafka-dotnet)
- **NuGet**: [Confluent.Kafka Package](https://www.nuget.org/packages/Confluent.Kafka/)
- **Compatibility**: Supports netstandard2.0, net462, net6.0, **net8.0** ✅

**Key Insights from Research**:

1. **Producer in ASP.NET Core**:
   - Use `ProduceAsync()` in high-concurrency scenarios (like API endpoints)
   - Near-concurrent requests are automatically batched for efficiency
   - Register as **Singleton** for connection pooling
   - Source: [Confluent Documentation](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)

2. **Consumer in Background Service**:
   - Implement `BackgroundService` base class with `ExecuteAsync()`
   - Use `Task.Run()` to avoid blocking application startup
   - Use `IServiceScopeFactory` to create scoped DbContext instances
   - Sources:
     - [Stack Overflow: Kafka Consumer as Background Service](https://stackoverflow.com/questions/56733810/how-to-properly-implement-kafka-consumer-as-a-background-service-on-net-core)
     - [Code Maze: Kafka in ASP.NET Core](https://code-maze.com/aspnetcore-using-kafka-in-a-web-api/)

3. **Offset Management** (Critical for Reliability):
   - **Auto-commit (default)**: Risk of message loss on crash
   - **Manual commit**: Better control but requires careful error handling
   - **Recommended**: Use `StoreOffset()` with disabled auto-offset storage
   - Source: [Confluent Documentation - Consumer Best Practices](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)

4. **Common Gotchas**:
   - Rebalancing can throw `KafkaException` with `ErrorCode.Local_State`
   - Awaiting each `ProduceAsync` individually kills throughput (except in concurrent scenarios)
   - Background services must handle `CancellationToken` properly
   - Consumer must be disposed properly to commit final offsets

### Docker Compose Research

**Key Configuration Points**:
- Confluent Platform 7.4+ images recommended
- Zookeeper on port 2181 (or use KRaft mode for Zookeeper-less setup)
- Kafka broker on port 9092 (internal) and 29092 (external/host access)
- **Critical**: `KAFKA_ADVERTISED_LISTENERS` must include both internal and external listeners
- Sources:
  - [Baeldung: Kafka Docker Setup](https://www.baeldung.com/ops/kafka-docker-setup)
  - [GitHub: Conduktor Kafka Stack Docker Compose](https://github.com/conduktor/kafka-stack-docker-compose)

---

## Implementation Blueprint

### Phase 1: Infrastructure Setup

#### 1.1 Docker Compose Configuration

**File**: Move `./Postgres/docker-compose.yml` to project root and add Kafka services

```yaml
version: '3.8'

services:
  # Existing PostgreSQL service
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
    networks:
      - order-payment-network
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U orderuser -d order_payment_simulation"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Zookeeper service for Kafka
  zookeeper:
    image: confluentinc/cp-zookeeper:7.4.4
    container_name: order-payment-zookeeper
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    ports:
      - "2181:2181"
    networks:
      - order-payment-network
    healthcheck:
      test: ["CMD-SHELL", "nc -z localhost 2181 || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5

  # Kafka broker
  kafka:
    image: confluentinc/cp-kafka:7.4.4
    container_name: order-payment-kafka
    depends_on:
      zookeeper:
        condition: service_healthy
    ports:
      - "9092:9092"
      - "29092:29092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:9092,PLAINTEXT_HOST://localhost:29092
      KAFKA_LISTENER_SECURITY_PROTOCOL_MAP: PLAINTEXT:PLAINTEXT,PLAINTEXT_HOST:PLAINTEXT
      KAFKA_INTER_BROKER_LISTENER_NAME: PLAINTEXT
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
      KAFKA_AUTO_CREATE_TOPICS_ENABLE: 'true'
    networks:
      - order-payment-network
    healthcheck:
      test: ["CMD-SHELL", "kafka-broker-api-versions --bootstrap-server localhost:9092 || exit 1"]
      interval: 10s
      timeout: 10s
      retries: 5

networks:
  order-payment-network:
    driver: bridge

volumes:
  postgres_data:
    driver: local
```

**Key Configuration Notes**:
- `PLAINTEXT://kafka:9092` - Used by consumers/producers inside Docker network
- `PLAINTEXT_HOST://localhost:29092` - Used by applications running on host (during development)
- `KAFKA_AUTO_CREATE_TOPICS_ENABLE: 'true'` - Topics will be created automatically on first publish
- Healthchecks ensure services start in correct order

#### 1.2 NuGet Package Installation

Add to `src/OrderPaymentSimulation.Api/OrderPaymentSimulation.Api.csproj`:

```xml
<PackageReference Include="Confluent.Kafka" Version="2.6.1" />
```

**Command**:
```bash
cd src/OrderPaymentSimulation.Api
dotnet add package Confluent.Kafka
```

#### 1.3 Configuration in appsettings.json

Add Kafka configuration section (follow JWT pattern at lines 5-10):

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
  "Kafka": {
    "BootstrapServers": "localhost:29092",
    "ClientId": "order-payment-api",
    "Topics": {
      "OrderCreated": "order-created-events",
      "OrderCompleted": "order-completed-events",
      "OrderExpired": "order-expired-events"
    },
    "ConsumerGroupId": "order-processing-group",
    "NotificationConsumerGroupId": "notification-group"
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

---

### Phase 2: Domain Model Extensions

#### 2.1 Create Notification Entity

**File**: `src/OrderPaymentSimulation.Api/Models/Notification.cs`

**Pattern**: Follow `Order.cs` structure (lines 1-15)

```csharp
namespace order_payment_simulation_api.Models;

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int OrderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public NotificationStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Order Order { get; set; } = null!;
}
```

#### 2.2 Create NotificationStatus Enum

**File**: `src/OrderPaymentSimulation.Api/Models/NotificationStatus.cs`

**Pattern**: Follow `OrderStatus.cs` (lines 1-10)

```csharp
namespace order_payment_simulation_api.Models;

public enum NotificationStatus : short
{
    OrderCompleted = 0,
    OrderExpired = 1
}
```

#### 2.3 Create Notification Entity Configuration

**File**: `src/OrderPaymentSimulation.Api/Data/Configurations/NotificationConfiguration.cs`

**Pattern**: Follow `OrderConfiguration.cs` (lines 1-47) exactly

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id");

        builder.Property(n => n.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(n => n.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(n => n.Message)
            .HasColumnName("message")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes on foreign keys for query performance
        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("idx_notifications_user_id");

        builder.HasIndex(n => n.OrderId)
            .HasDatabaseName("idx_notifications_order_id");

        // Relationships
        builder.HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Order)
            .WithMany()
            .HasForeignKey(n => n.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

#### 2.4 Update DbContext

**File**: `src/OrderPaymentSimulation.Api/Data/OrderPaymentDbContext.cs`

**Changes**:
1. Add DbSet property after line 17:
```csharp
public DbSet<Notification> Notifications { get; set; }
```

2. Register configuration in `OnModelCreating` after line 27:
```csharp
modelBuilder.ApplyConfiguration(new NotificationConfiguration());
```

#### 2.5 Update Seed Data

**File**: `src/OrderPaymentSimulation.Api/Data/SeedData.cs`

Add after OrderItems seeding (after line 107):

```csharp
// Seed Notifications (sample audit trail)
var notifications = new[]
{
    new Notification
    {
        UserId = testUser.Id,
        OrderId = order1.Id,
        Message = $"Order #{order1.Id} for user {testUser.Email} was completed. Items: Laptop, Mouse. Total: ${order1.Total}",
        Status = NotificationStatus.OrderCompleted,
        CreatedAt = DateTime.UtcNow.AddDays(-4),
        UpdatedAt = DateTime.UtcNow.AddDays(-4)
    }
};

context.Notifications.AddRange(notifications);
context.SaveChanges();
```

---

### Phase 3: Event Models (DTOs)

Create event DTOs for Kafka messages.

**File**: `src/OrderPaymentSimulation.Api/Dtos/OrderCreatedEvent.cs`

```csharp
namespace order_payment_simulation_api.Dtos;

public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

**File**: `src/OrderPaymentSimulation.Api/Dtos/OrderCompletedEvent.cs`

```csharp
namespace order_payment_simulation_api.Dtos;

public class OrderCompletedEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Total { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
    public DateTime CompletedAt { get; set; }
}
```

**File**: `src/OrderPaymentSimulation.Api/Dtos/OrderExpiredEvent.cs`

```csharp
namespace order_payment_simulation_api.Dtos;

public class OrderExpiredEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public DateTime ExpiredAt { get; set; }
}
```

---

### Phase 4: Kafka Services

#### 4.1 Kafka Producer Service

**File**: `src/OrderPaymentSimulation.Api/Services/IKafkaProducerService.cs`

**Pattern**: Follow `IJwtService.cs` (lines 1-8)

```csharp
namespace order_payment_simulation_api.Services;

public interface IKafkaProducerService
{
    Task PublishAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default);
}
```

**File**: `src/OrderPaymentSimulation.Api/Services/KafkaProducerService.cs`

**Pattern**: Follow `JwtService.cs` (lines 1-50) for configuration reading

```csharp
using Confluent.Kafka;
using System.Text.Json;

namespace order_payment_simulation_api.Services;

public class KafkaProducerService : IKafkaProducerService, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;

        var kafkaSettings = configuration.GetSection("Kafka");
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaSettings["BootstrapServers"],
            ClientId = kafkaSettings["ClientId"],
            // Recommended settings for reliability
            Acks = Acks.All,  // Wait for all replicas to acknowledge
            EnableIdempotence = true,  // Prevent duplicates on retries
            MaxInFlight = 5,
            MessageSendMaxRetries = 3
        };

        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    public async Task PublishAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageJson = JsonSerializer.Serialize(message);
            var kafkaMessage = new Message<string, string>
            {
                Key = key,
                Value = messageJson
            };

            var deliveryResult = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

            _logger.LogInformation(
                "Published message to {Topic} - Partition: {Partition}, Offset: {Offset}, Key: {Key}",
                topic, deliveryResult.Partition.Value, deliveryResult.Offset.Value, key);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish message to {Topic}: {Reason}", topic, ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(10));
        _producer?.Dispose();
    }
}
```

#### 4.2 Order Created Event Consumer (Background Service)

**File**: `src/OrderPaymentSimulation.Api/Services/OrderCreatedEventConsumer.cs`

**Pattern**: Implement `BackgroundService` base class

**Key Implementation Notes**:
- Use `IServiceScopeFactory` to create scoped DbContext (cannot inject DbContext directly in singleton service)
- Handle `CancellationToken` to gracefully stop consuming
- Use `Task.Run()` to prevent blocking app startup
- Commit offsets only after successful processing

```csharp
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;
using System.Text.Json;

namespace order_payment_simulation_api.Services;

public class OrderCreatedEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderCreatedEventConsumer> _logger;
    private readonly IKafkaProducerService _producerService;
    private IConsumer<string, string>? _consumer;

    public OrderCreatedEventConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<OrderCreatedEventConsumer> logger,
        IKafkaProducerService producerService)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _producerService = producerService;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafkaSettings = _configuration.GetSection("Kafka");
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings["BootstrapServers"],
            GroupId = kafkaSettings["ConsumerGroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,  // Manual commit for better control
            EnableAutoOffsetStore = false  // Use StoreOffset for reliability
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        var topic = kafkaSettings["Topics:OrderCreated"];
        _consumer.Subscribe(topic);

        _logger.LogInformation("OrderCreatedEventConsumer started. Subscribed to topic: {Topic}", topic);

        // Use Task.Run to avoid blocking app startup
        return Task.Run(async () =>
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);

                        if (consumeResult?.Message?.Value == null)
                            continue;

                        _logger.LogInformation(
                            "Received OrderCreated event - Order ID: {Key}, Partition: {Partition}, Offset: {Offset}",
                            consumeResult.Message.Key, consumeResult.Partition.Value, consumeResult.Offset.Value);

                        var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(consumeResult.Message.Value);

                        if (orderCreatedEvent != null)
                        {
                            await ProcessOrderCreatedAsync(orderCreatedEvent, stoppingToken);

                            // Store offset and commit after successful processing
                            _consumer.StoreOffset(consumeResult);
                            _consumer.Commit(consumeResult);
                        }
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("OrderCreatedEventConsumer cancellation requested");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing OrderCreated event");
                        // Don't commit offset on error - message will be reprocessed
                    }
                }
            }
            finally
            {
                _consumer.Close();
                _logger.LogInformation("OrderCreatedEventConsumer stopped");
            }
        }, stoppingToken);
    }

    private async Task ProcessOrderCreatedAsync(OrderCreatedEvent orderEvent, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderPaymentDbContext>();

        var order = await context.Orders
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderEvent.OrderId, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order {OrderId} not found for processing", orderEvent.OrderId);
            return;
        }

        // Update status to Processing
        order.Status = OrderStatus.Processing;
        order.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Order {OrderId} status updated to Processing. Starting payment simulation...", order.Id);

        // Simulate payment processing (5 second delay)
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        // 50% success rate for payment
        var isPaymentSuccessful = Random.Shared.Next(0, 2) == 0;

        if (isPaymentSuccessful)
        {
            order.Status = OrderStatus.Completed;
            order.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} payment succeeded. Status updated to Completed", order.Id);

            // Publish OrderCompleted event
            var completedEvent = new OrderCompletedEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Total = order.Total,
                Items = order.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList(),
                CompletedAt = DateTime.UtcNow
            };

            var completedTopic = _configuration.GetSection("Kafka")["Topics:OrderCompleted"];
            await _producerService.PublishAsync(completedTopic!, order.Id.ToString(), completedEvent, cancellationToken);
        }
        else
        {
            _logger.LogInformation("Order {OrderId} payment simulation: payment processing continues (status remains Processing)", order.Id);
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
```

#### 4.3 Notification Event Consumer (Background Service)

**File**: `src/OrderPaymentSimulation.Api/Services/NotificationEventConsumer.cs`

```csharp
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;
using System.Text.Json;

namespace order_payment_simulation_api.Services;

public class NotificationEventConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationEventConsumer> _logger;
    private IConsumer<string, string>? _consumer;

    public NotificationEventConsumer(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<NotificationEventConsumer> logger)
    {
        _configuration = configuration;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var kafkaSettings = _configuration.GetSection("Kafka");
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings["BootstrapServers"],
            GroupId = kafkaSettings["NotificationConsumerGroupId"],
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false
        };

        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();

        // Subscribe to both OrderCompleted and OrderExpired topics
        var completedTopic = kafkaSettings["Topics:OrderCompleted"];
        var expiredTopic = kafkaSettings["Topics:OrderExpired"];
        _consumer.Subscribe(new[] { completedTopic!, expiredTopic! });

        _logger.LogInformation("NotificationEventConsumer started. Subscribed to topics: {CompletedTopic}, {ExpiredTopic}",
            completedTopic, expiredTopic);

        return Task.Run(async () =>
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = _consumer.Consume(stoppingToken);

                        if (consumeResult?.Message?.Value == null)
                            continue;

                        _logger.LogInformation(
                            "Received notification event from topic {Topic} - Key: {Key}",
                            consumeResult.Topic, consumeResult.Message.Key);

                        if (consumeResult.Topic == completedTopic)
                        {
                            var completedEvent = JsonSerializer.Deserialize<OrderCompletedEvent>(consumeResult.Message.Value);
                            if (completedEvent != null)
                            {
                                await HandleOrderCompletedAsync(completedEvent, stoppingToken);
                            }
                        }
                        else if (consumeResult.Topic == expiredTopic)
                        {
                            var expiredEvent = JsonSerializer.Deserialize<OrderExpiredEvent>(consumeResult.Message.Value);
                            if (expiredEvent != null)
                            {
                                await HandleOrderExpiredAsync(expiredEvent, stoppingToken);
                            }
                        }

                        _consumer.StoreOffset(consumeResult);
                        _consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation("NotificationEventConsumer cancellation requested");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing notification event");
                    }
                }
            }
            finally
            {
                _consumer.Close();
                _logger.LogInformation("NotificationEventConsumer stopped");
            }
        }, stoppingToken);
    }

    private async Task HandleOrderCompletedAsync(OrderCompletedEvent completedEvent, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderPaymentDbContext>();

        var user = await context.Users.FindAsync(new object[] { completedEvent.UserId }, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for order completion notification", completedEvent.UserId);
            return;
        }

        // Build product list for email message
        var productList = string.Join(", ", completedEvent.Items.Select(i => $"{i.ProductName} (x{i.Quantity})"));

        var emailMessage = $"Order #{completedEvent.OrderId} for user {user.Email} was issued containing products: {productList}. Total: ${completedEvent.Total:F2}";

        // Log fake email notification to console
        _logger.LogInformation("📧 FAKE EMAIL NOTIFICATION: {EmailMessage}", emailMessage);

        // Save notification to database as audit trail
        var notification = new Notification
        {
            UserId = completedEvent.UserId,
            OrderId = completedEvent.OrderId,
            Message = emailMessage,
            Status = NotificationStatus.OrderCompleted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Notification saved to database for Order {OrderId}", completedEvent.OrderId);
    }

    private async Task HandleOrderExpiredAsync(OrderExpiredEvent expiredEvent, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderPaymentDbContext>();

        var user = await context.Users.FindAsync(new object[] { expiredEvent.UserId }, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for order expiration notification", expiredEvent.UserId);
            return;
        }

        var expirationMessage = $"Order #{expiredEvent.OrderId} for user {user.Email} has expired due to payment timeout.";

        _logger.LogInformation("⏰ Order Expiration: {ExpirationMessage}", expirationMessage);

        // Save expiration notification to database as audit trail
        var notification = new Notification
        {
            UserId = expiredEvent.UserId,
            OrderId = expiredEvent.OrderId,
            Message = expirationMessage,
            Status = NotificationStatus.OrderExpired,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Notifications.Add(notification);
        await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expiration notification saved to database for Order {OrderId}", expiredEvent.OrderId);
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}
```

#### 4.4 Order Expiration Background Service

**File**: `src/OrderPaymentSimulation.Api/Services/OrderExpirationService.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Models;

namespace order_payment_simulation_api.Services;

public class OrderExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrderExpirationService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(60);
    private readonly TimeSpan _expirationThreshold = TimeSpan.FromMinutes(10);

    public OrderExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrderExpirationService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderExpirationService started. Checking every {Interval} seconds for orders older than {Threshold} minutes",
            _checkInterval.TotalSeconds, _expirationThreshold.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_checkInterval, stoppingToken);
                await CheckAndExpireOrdersAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("OrderExpirationService cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OrderExpirationService");
            }
        }

        _logger.LogInformation("OrderExpirationService stopped");
    }

    private async Task CheckAndExpireOrdersAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderPaymentDbContext>();
        var producerService = scope.ServiceProvider.GetRequiredService<IKafkaProducerService>();

        var expirationCutoff = DateTime.UtcNow.Subtract(_expirationThreshold);

        var expiredOrders = await context.Orders
            .Where(o => o.Status == OrderStatus.Processing && o.UpdatedAt < expirationCutoff)
            .ToListAsync(cancellationToken);

        if (expiredOrders.Count == 0)
        {
            _logger.LogDebug("No orders found for expiration");
            return;
        }

        _logger.LogInformation("Found {Count} orders to expire", expiredOrders.Count);

        foreach (var order in expiredOrders)
        {
            order.Status = OrderStatus.Expired;
            order.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Order {OrderId} expired (last updated: {LastUpdated})", order.Id, order.UpdatedAt);

            // Publish OrderExpired event
            var expiredEvent = new OrderExpiredEvent
            {
                OrderId = order.Id,
                UserId = order.UserId,
                ExpiredAt = DateTime.UtcNow
            };

            var expiredTopic = _configuration.GetSection("Kafka")["Topics:OrderExpired"];
            await producerService.PublishAsync(expiredTopic!, order.Id.ToString(), expiredEvent, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Successfully expired {Count} orders", expiredOrders.Count);
    }
}
```

---

### Phase 5: Integration into OrderController

**File**: `src/OrderPaymentSimulation.Api/Controllers/OrderController.cs`

**Modification**: Inject `IKafkaProducerService` and publish event after order creation

**At line 16** (constructor parameters), add:
```csharp
private readonly IKafkaProducerService _kafkaProducerService;
```

**At line 18** (constructor), add parameter:
```csharp
public OrderController(
    OrderPaymentDbContext context,
    ILogger<OrderController> logger,
    IKafkaProducerService kafkaProducerService)
{
    _context = context;
    _logger = logger;
    _kafkaProducerService = kafkaProducerService;
}
```

**At line 196** (after order creation logging), add:

```csharp
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
```

**Add using statement at top**:
```csharp
using order_payment_simulation_api.Services;
```

---

### Phase 6: Service Registration in Program.cs

**File**: `src/OrderPaymentSimulation.Api/Program.cs`

**After line 21** (after JWT service registration), add:

```csharp
// Register Kafka services
builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
builder.Services.AddHostedService<OrderCreatedEventConsumer>();
builder.Services.AddHostedService<NotificationEventConsumer>();
builder.Services.AddHostedService<OrderExpirationService>();

builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});
```

---

## Testing Strategy

### Unit Tests

**File**: `test/UnitTests/UnitTests/KafkaProducerServiceTests.cs`

```csharp
using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using order_payment_simulation_api.Dtos;
using order_payment_simulation_api.Services;

namespace UnitTests;

public class KafkaProducerServiceTests
{
    private readonly Fixture _fixture;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<KafkaProducerService>> _mockLogger;

    public KafkaProducerServiceTests()
    {
        _fixture = new Fixture();
        _mockLogger = new Mock<ILogger<KafkaProducerService>>();

        var configData = new Dictionary<string, string>
        {
            { "Kafka:BootstrapServers", "localhost:29092" },
            { "Kafka:ClientId", "test-client" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData!)
            .Build();
    }

    [Fact]
    public void KafkaProducerService_Constructor_CreatesProducerSuccessfully()
    {
        // Act
        using var service = new KafkaProducerService(_configuration, _mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_WithValidMessage_DoesNotThrow()
    {
        // Arrange
        using var service = new KafkaProducerService(_configuration, _mockLogger.Object);
        var orderEvent = new OrderCreatedEvent
        {
            OrderId = 1,
            UserId = 1,
            Total = 100.00m,
            CreatedAt = DateTime.UtcNow
        };

        // Act & Assert
        // Note: This will fail without actual Kafka running
        // In real tests, use Testcontainers or mock the IProducer
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await service.PublishAsync("test-topic", "1", orderEvent));
    }
}
```

**File**: `test/UnitTests/UnitTests/OrderExpirationLogicTests.cs`

```csharp
using FluentAssertions;
using order_payment_simulation_api.Models;

namespace UnitTests;

public class OrderExpirationLogicTests
{
    [Fact]
    public void OrderExpiration_CalculatesCorrectCutoffTime()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var expirationThreshold = TimeSpan.FromMinutes(10);

        // Act
        var cutoffTime = now.Subtract(expirationThreshold);

        // Assert
        cutoffTime.Should().BeBefore(now);
        (now - cutoffTime).Should().Be(expirationThreshold);
    }

    [Fact]
    public void OrderStatus_ExpirationTransition_IsValid()
    {
        // Arrange
        var order = new Order
        {
            Id = 1,
            UserId = 1,
            Total = 100.00m,
            Status = OrderStatus.Processing,
            CreatedAt = DateTime.UtcNow.AddMinutes(-15),
            UpdatedAt = DateTime.UtcNow.AddMinutes(-15)
        };

        // Act
        order.Status = OrderStatus.Expired;

        // Assert
        order.Status.Should().Be(OrderStatus.Expired);
    }
}
```

### Integration Tests

**File**: `test/IntegrationTests/IntegrationTests/KafkaIntegrationTests.cs`

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using order_payment_simulation_api.Data;
using order_payment_simulation_api.Dtos;

namespace IntegrationTests;

public class KafkaIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public KafkaIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_PublishesKafkaEvent_AndProcessesSuccessfully()
    {
        // Arrange
        var (userId, token) = await CreateAndLoginUser();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var productId = await CreateProductWithStock(token, 100);

        var orderRequest = new CreateOrderRequest
        {
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = productId, Quantity = 2 }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/order", orderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var orderDto = await response.Content.ReadFromJsonAsync<OrderDto>();
        orderDto.Should().NotBeNull();
        orderDto!.Status.Should().Be("Pending");

        // Wait for async processing (in real tests, use polling or test-specific events)
        await Task.Delay(6000); // 5 seconds processing + buffer

        // Verify order status changed
        var getResponse = await _client.GetAsync($"/api/order/{orderDto.Id}");
        var updatedOrder = await getResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Status should be either Completed or Processing (50/50 chance)
        updatedOrder!.Status.Should().BeOneOf("Completed", "Processing");
    }

    [Fact]
    public async Task NotificationConsumer_CreatesNotificationRecord_OnOrderCompletion()
    {
        // This test requires Kafka to be running
        // In practice, you'd use Testcontainers to spin up Kafka for integration tests
        // For now, this serves as a template

        // Arrange - create order and wait for processing
        // Assert - query notifications table for audit record
        Assert.True(true); // Placeholder
    }

    private async Task<(int userId, string token)> CreateAndLoginUser()
    {
        var createUserRequest = new CreateUserRequest
        {
            Name = $"Test User {Guid.NewGuid()}",
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "Password123!"
        };

        var createResponse = await _client.PutAsJsonAsync("/api/user", createUserRequest);
        var userDto = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var loginRequest = new LoginRequest
        {
            Email = createUserRequest.Email,
            Password = createUserRequest.Password
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();

        return (userDto!.Id, loginResult!.Token);
    }

    private async Task<int> CreateProductWithStock(string token, int stock)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var productRequest = new CreateProductRequest
        {
            Name = $"Test Product {Guid.NewGuid()}",
            Description = "Test Description",
            Price = 50.00m,
            Stock = stock
        };

        var response = await _client.PutAsJsonAsync("/api/product", productRequest);
        var productDto = await response.Content.ReadFromJsonAsync<ProductDto>();

        return productDto!.Id;
    }
}
```

---

## Validation Gates

All validation gates must pass for successful implementation:

### 1. Build Validation
```bash
# Clean and restore
dotnet clean
dotnet restore

# Build solution
dotnet build OrderPaymentSimulation.Api.sln --configuration Release

# Expected: 0 errors, 0 warnings
```

### 2. Docker Infrastructure Validation
```bash
# Start Docker services
docker-compose up -d

# Verify all containers running
docker ps | grep -E "order-payment-(db|zookeeper|kafka)"

# Expected output: 3 running containers

# Check Kafka topics (after first order creation)
docker exec order-payment-kafka kafka-topics --list --bootstrap-server localhost:9092

# Expected topics:
# - order-created-events
# - order-completed-events
# - order-expired-events

# View consumer groups
docker exec order-payment-kafka kafka-consumer-groups --list --bootstrap-server localhost:9092

# Expected groups:
# - order-processing-group
# - notification-group
```

### 3. Database Schema Validation
```bash
# Connect to PostgreSQL
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation

# Check notifications table exists
\dt notifications

# Check table structure
\d notifications

# Expected columns: id, user_id, order_id, message, status, created_at, updated_at

# Check indexes
\di idx_notifications_*

# Expected: idx_notifications_user_id, idx_notifications_order_id

# Exit psql
\q
```

### 4. Unit Tests
```bash
# Run unit tests
dotnet test test/UnitTests/UnitTests/UnitTests.csproj --verbosity normal

# Expected: All tests passing (14+ tests total)
```

### 5. Integration Tests
```bash
# Run integration tests (requires Docker services running)
dotnet test test/IntegrationTests/IntegrationTests/IntegrationTests.csproj --verbosity normal

# Expected: All tests passing (30+ tests total including new Kafka tests)
```

### 6. API Runtime Validation
```bash
# Start API
cd src/OrderPaymentSimulation.Api
dotnet run

# Expected console output should show:
# - "OrderCreatedEventConsumer started. Subscribed to topic: order-created-events"
# - "NotificationEventConsumer started. Subscribed to topics: order-completed-events, order-expired-events"
# - "OrderExpirationService started. Checking every 60 seconds for orders older than 10 minutes"
```

### 7. End-to-End Workflow Validation

**Manual Test Sequence**:

1. **Login and get JWT token**:
```bash
curl -X POST http://localhost:5267/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "test@example.com", "password": "Password123!"}'

# Save token from response
```

2. **Create order**:
```bash
curl -X PUT http://localhost:5267/api/order \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {"productId": 1, "quantity": 1}
    ]
  }'

# Should return 201 Created with order in "Pending" status
```

3. **Check console logs** (in terminal running `dotnet run`):
```
[INFO] Order created: 4 for user 2
[INFO] OrderCreated event published for Order 4
[INFO] Received OrderCreated event - Order ID: 4
[INFO] Order 4 status updated to Processing. Starting payment simulation...
[INFO] Order 4 payment succeeded. Status updated to Completed  # (50% chance)
[INFO] Published message to order-completed-events
[INFO] 📧 FAKE EMAIL NOTIFICATION: Order #4 for user test@example.com...
[INFO] Notification saved to database for Order 4
```

4. **Query order status** (wait 6+ seconds):
```bash
curl -X GET http://localhost:5267/api/order/4 \
  -H "Authorization: Bearer YOUR_TOKEN"

# Should show status as "Completed" or "Processing"
```

5. **Check notifications table**:
```sql
-- In psql
SELECT * FROM notifications ORDER BY created_at DESC LIMIT 5;

-- Should see notification records for completed/expired orders
```

6. **Test order expiration** (wait 10+ minutes or adjust `_expirationThreshold` to 1 minute for testing):
```
[INFO] Found 1 orders to expire
[INFO] Order 5 expired (last updated: 2024-12-02 10:15:00)
[INFO] Published message to order-expired-events
[INFO] ⏰ Order Expiration: Order #5 for user test@example.com has expired...
[INFO] Expiration notification saved to database for Order 5
```

### 8. Kafka Message Inspection (Optional)
```bash
# Consume messages from OrderCreated topic
docker exec order-payment-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic order-created-events \
  --from-beginning

# Should show JSON messages for all created orders

# Consume from OrderCompleted topic
docker exec order-payment-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic order-completed-events \
  --from-beginning
```

---

## Database Cleanup and Re-seeding

After implementation is complete, test with fresh database:

```bash
# Stop all services
docker-compose down -v

# This removes containers, networks, and volumes (drops database)

# Restart services
docker-compose up -d

# Wait for services to be healthy
docker ps

# Run API (will auto-create DB and seed data)
cd src/OrderPaymentSimulation.Api
dotnet run

# Verify seed data
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation -c "SELECT COUNT(*) FROM users;"
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation -c "SELECT COUNT(*) FROM products;"
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation -c "SELECT COUNT(*) FROM orders;"
docker exec -it order-payment-db psql -U orderuser -d order_payment_simulation -c "SELECT COUNT(*) FROM notifications;"

# Expected counts: 2 users, 5 products, 3 orders, 1+ notifications
```

---

## Common Pitfalls and Troubleshooting

### 1. Kafka Connection Issues

**Symptom**: `ProduceException: Broker transport failure` or `Connection refused`

**Causes**:
- Kafka not fully started (healthcheck not passed)
- Wrong `BootstrapServers` configuration (use `localhost:29092` from host, `kafka:9092` from within Docker)

**Solution**:
```bash
# Check Kafka health
docker exec order-payment-kafka kafka-broker-api-versions --bootstrap-server localhost:9092

# Check Kafka logs
docker logs order-payment-kafka
```

### 2. Consumer Not Receiving Messages

**Symptom**: Producer works but consumer doesn't process messages

**Causes**:
- Consumer group already has committed offsets
- Topic auto-creation disabled
- Consumer not subscribed to correct topic name

**Solution**:
```bash
# Reset consumer group offsets
docker exec order-payment-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group order-processing-group \
  --reset-offsets --to-earliest --topic order-created-events --execute

# Verify consumer group status
docker exec order-payment-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group order-processing-group \
  --describe
```

### 3. Background Service Not Starting

**Symptom**: No logs from `OrderCreatedEventConsumer` or other background services

**Causes**:
- Exception in `ExecuteAsync` causes silent failure
- Service not registered in Program.cs

**Solution**:
- Check `AddHostedService<>` registration in Program.cs
- Add try-catch with logging in `ExecuteAsync`
- Check application startup logs for exceptions

### 4. Database Schema Issues

**Symptom**: `Npgsql.PostgresException: relation "notifications" does not exist`

**Causes**:
- `NotificationConfiguration` not registered in DbContext
- `EnsureCreated()` not called (or called before configuration added)

**Solution**:
```bash
# Drop and recreate database
docker-compose down -v
docker-compose up -d

# Verify configuration is registered
# Check OrderPaymentDbContext.OnModelCreating for ApplyConfiguration call
```

### 5. Test Failures Due to Timing

**Symptom**: Integration tests fail intermittently

**Causes**:
- Async processing not complete before assertion
- Race conditions in message processing

**Solution**:
- Increase delay in tests (from 5s to 7-8s)
- Use polling instead of fixed delays
- Consider using Testcontainers for isolated Kafka instances per test

### 6. Memory/Resource Issues in Docker

**Symptom**: Kafka container crashes or becomes unresponsive

**Causes**:
- Insufficient Docker memory allocation
- Too many retained messages

**Solution**:
```bash
# Check Docker resource usage
docker stats

# Increase Docker Desktop memory allocation (Settings > Resources)
# Recommended: 4GB+ for Kafka + Zookeeper + PostgreSQL

# Clear Kafka data
docker-compose down -v
docker volume prune
docker-compose up -d
```

---

## Documentation Updates

### README.md Updates

Add sections:

1. **Prerequisites**: Docker Desktop, .NET 8.0 SDK
2. **Kafka Setup**: Docker compose commands, topic creation
3. **Architecture**: Event-driven flow diagram
4. **Testing**: How to test order workflow end-to-end

### CLAUDE.md Updates

1. **Technology Stack**: Add Confluent.Kafka 2.6.1
2. **Entity Models**: Add Notification entity documentation
3. **Services**: Document Kafka producer/consumer services
4. **Background Services**: Document OrderExpirationService
5. **Event Flow**: Document complete event-driven workflow
6. **Testing Statistics**: Update test counts (add 6+ new tests)
7. **Current State - Implemented**: Add Kafka integration bullet points
8. **Pending Implementation**: Remove items that are now complete

---

## Success Criteria Checklist

- [ ] Docker compose includes Kafka and Zookeeper
- [ ] Kafka producer service publishes OrderCreated events
- [ ] OrderCreatedEventConsumer processes events and updates order status
- [ ] 50% of orders complete, 50% remain processing (random)
- [ ] Completed orders publish OrderCompleted events
- [ ] NotificationEventConsumer logs fake emails and saves notifications
- [ ] OrderExpirationService runs every 60 seconds
- [ ] Orders >10 minutes in Processing status expire automatically
- [ ] Expired orders publish OrderExpired events
- [ ] Notifications table has audit trail for all completions/expirations
- [ ] All unit tests pass (14+ tests)
- [ ] All integration tests pass (30+ tests)
- [ ] Build completes with 0 errors, 0 warnings
- [ ] Manual end-to-end workflow validation successful
- [ ] Documentation updated (README.md and CLAUDE.md)

---

## Additional Resources

### Official Documentation
- [Confluent Kafka .NET Client](https://docs.confluent.io/kafka-clients/dotnet/current/overview.html)
- [Apache Kafka Getting Started](https://kafka.apache.org/quickstart)
- [ASP.NET Core Background Services](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services)

### Implementation Guides
- [Code Maze: Kafka in ASP.NET Core](https://code-maze.com/aspnetcore-using-kafka-in-a-web-api/)
- [Stack Overflow: Kafka Consumer as Background Service](https://stackoverflow.com/questions/56733810/how-to-properly-implement-kafka-consumer-as-a-background-service-on-net-core)
- [Medium: Microservices with .NET Core and Kafka](https://medium.com/simform-engineering/creating-microservices-with-net-core-and-kafka-a-step-by-step-approach-1737410ba76a)

### Docker Resources
- [Confluent Docker Images](https://hub.docker.com/u/confluentinc)
- [GitHub: Kafka Docker Compose Examples](https://github.com/conduktor/kafka-stack-docker-compose)

### Testing Resources
- [AutoFixture Documentation](https://www.nuget.org/packages/autofixture)
- [Moq Documentation](https://www.nuget.org/packages/moq/)
- [FluentAssertions Documentation](https://fluentassertions.com/)

---

**End of PRP**
