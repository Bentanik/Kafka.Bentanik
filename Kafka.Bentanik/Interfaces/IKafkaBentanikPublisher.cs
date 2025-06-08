namespace Kafka.Bentanik.Interfaces;

public interface IKafkaBentanikPublisher
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
}
