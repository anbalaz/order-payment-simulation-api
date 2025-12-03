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
