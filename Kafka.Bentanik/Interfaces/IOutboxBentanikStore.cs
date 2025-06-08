namespace Kafka.Bentanik.Interfaces;

public interface IOutboxBentanikStore
{
    Task<List<OutboxBentanikMessage>> GetUnsentMessagesAsync(int maxCount, CancellationToken ct);
    Task MarkAsSentAsync(string messageId, CancellationToken ct);
    Task IncrementRetryAsync(string messageId, CancellationToken ct);
    Task MoveToDeadLetterAsync(string messageId, CancellationToken ct);
}