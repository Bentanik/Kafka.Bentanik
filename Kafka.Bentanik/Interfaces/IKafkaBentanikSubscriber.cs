namespace Kafka.Bentanik.Interfaces;

public interface IKafkaBentanikSubscriber<T>
{
    Task HandleAsync(T message, CancellationToken cancellationToken);
}
