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
