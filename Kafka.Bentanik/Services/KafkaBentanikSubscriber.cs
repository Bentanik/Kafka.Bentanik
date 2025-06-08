namespace Kafka.Bentanik.Services;

public class KafkaBentanikSubscriber<T> : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaBentanikSubscriberOptions<T> _options;
    private readonly ILogger<KafkaBentanikSubscriber<T>> _logger;

    public KafkaBentanikSubscriber(
        IServiceProvider serviceProvider,
        IOptions<KafkaBentanikSubscriberOptions<T>> options,
        ILogger<KafkaBentanikSubscriber<T>> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers ?? "localhost:9092",
            GroupId = _options.GroupId ?? "default-group",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _options.ConfigureConsumer?.Invoke(config);

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_options.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(100));

                if (result?.Message?.Value == null)
                {
                    await Task.Delay(200, stoppingToken);
                    continue;
                }

                T? message;
                try
                {
                    message = JsonSerializer.Deserialize<T>(result.Message.Value);
                    if (message == null)
                    {
                        _logger.LogWarning("Message deserialized to null from topic [{topic}]", _options.Topic);
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize message from topic [{topic}]", _options.Topic);
                    continue;
                }

                var attempt = 0;
                var success = false;

                while (attempt < (_options.MaxRetryAttempts ?? 3) && !success)
                {
                    attempt++;
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var handler = scope.ServiceProvider.GetRequiredService<IKafkaBentanikSubscriber<T>>();
                        await handler.HandleAsync(message, stoppingToken);

                        consumer.Commit(result); // commit sau khi xử lý OK
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Attempt {Attempt} failed for topic {Topic}", attempt, _options.Topic);

                        if (attempt >= (_options.MaxRetryAttempts ?? 3))
                        {
                            if (_options.OnError != null)
                                await _options.OnError(ex, message);

                            if (!string.IsNullOrEmpty(_options.DeadLetterTopic))
                            {
                                try
                                {
                                    var dlqPublisher = new KafkaBentanikPublisher(new ProducerConfig
                                    {
                                        BootstrapServers = _options.BootstrapServers
                                    });
                                    await dlqPublisher.PublishAsync(_options.DeadLetterTopic, message);
                                }
                                catch (Exception dlqEx)
                                {
                                    _logger.LogError(dlqEx, "Failed to send to DLQ [{topic}]", _options.DeadLetterTopic);
                                }
                            }
                        }
                        else
                        {
                            await Task.Delay(_options.RetryDelay, stoppingToken);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Kafka subscriber shutting down for topic [{topic}]", _options.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in Kafka subscriber for topic [{topic}]", _options.Topic);
        }
        finally
        {
            consumer.Unsubscribe();
            consumer.Close();
        }
    }
}
