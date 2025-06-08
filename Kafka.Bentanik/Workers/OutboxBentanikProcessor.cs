namespace Kafka.Bentanik.Workers;
public class OutboxBentanikProcessor : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<OutboxBentanikProcessor> _logger;

    private const int BatchSize = 20;
    private const int MaxRetryCount = 5;
    private static readonly TimeSpan DelayInterval = TimeSpan.FromSeconds(1);

    public OutboxBentanikProcessor(IServiceProvider provider, ILogger<OutboxBentanikProcessor> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("OutboxBentanikProcessor started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = _provider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<IOutboxBentanikStore>();
                var publisher = scope.ServiceProvider.GetRequiredService<IKafkaBentanikPublisher>();

                var messages = await store.GetUnsentMessagesAsync(BatchSize, ct);

                foreach (var msg in messages)
                {
                    try
                    {
                        await publisher.PublishAsync(msg.Topic, msg.Payload, ct);
                        await store.MarkAsSentAsync(msg.Id, ct);

                        _logger.LogInformation("Published: {EventType}, ID: {Id}", msg.EventType, msg.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish outbox message {Id} (RetryCount: {Retry})", msg.Id, msg.RetryCount);

                        await store.IncrementRetryAsync(msg.Id, ct);

                        if (msg.RetryCount + 1 >= MaxRetryCount)
                        {
                            _logger.LogWarning("Moving message {Id} to Dead Letter Queue", msg.Id);
                            await store.MoveToDeadLetterAsync(msg.Id, ct);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in OutboxBentanikProcessor.");
            }

            await Task.Delay(DelayInterval, ct);
        }

        _logger.LogInformation("OutboxBentanikProcessor stopped.");
    }
}