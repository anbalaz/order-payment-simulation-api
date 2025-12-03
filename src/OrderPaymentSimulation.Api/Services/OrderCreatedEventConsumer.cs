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
