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