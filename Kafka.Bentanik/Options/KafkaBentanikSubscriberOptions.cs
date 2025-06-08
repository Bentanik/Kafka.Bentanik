namespace Kafka.Bentanik.Options;

public class KafkaBentanikSubscriberOptions<T>
{
    public string Topic { get; set; } = default!;
    public string GroupId { get; set; } = "default-group";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(3);
    public string? DeadLetterTopic { get; set; }
    public Func<Exception, T, Task>? OnError { get; set; }
    public Action<ConsumerConfig>? ConfigureConsumer { get; set; }
}