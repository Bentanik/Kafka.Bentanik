namespace Kafka.Bentanik.Models;

public class OutboxMessage
{
    public string Id { get; set; }
    public string Topic { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}