namespace order_payment_simulation_api.Services;

public interface IKafkaProducerService
{
    Task PublishAsync<T>(string topic, string key, T message, CancellationToken cancellationToken = default);
}
