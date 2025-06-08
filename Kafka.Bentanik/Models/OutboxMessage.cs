namespace Kafka.Bentanik.Models;

public class OutboxMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public string Topic { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool Processed { get; set; } = false;
    public int RetryCount { get; set; } = 0;
    public string? Status { get; set; } = "Pending"; // Pending, Failed, Success, DeadLettered
    public DateTime? ProcessedAt { get; set; }

    public bool IsDeadLettered => Status == "DeadLettered";
}