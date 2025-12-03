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
