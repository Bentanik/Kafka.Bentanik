namespace Kafka.Bentanik.Services;

public class KafkaBentanikSubscriber<T> : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaBentanikSubscriberOptions<T> _options;

    public KafkaBentanikSubscriber(IServiceProvider serviceProvider, IOptions<KafkaBentanikSubscriberOptions<T>> options)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers ?? "localhost:9092",
            GroupId = _options.GroupId ?? "default-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        _options.ConfigureConsumer?.Invoke(config);

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(_options.Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(stoppingToken);
                var message = JsonSerializer.Deserialize<T>(result.Message.Value);

                if (message != null)
                {
                    var attempt = 0;
                    bool success = false;

                    while (attempt < _options.MaxRetryAttempts && !success)
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var handler = scope.ServiceProvider.GetRequiredService<IKafkaBentanikSubscriber<T>>();
                            await handler.HandleAsync(message, stoppingToken);
                            success = true;
                        }
                        catch (Exception ex)
                        {
                            attempt++;
                            if (attempt >= _options.MaxRetryAttempts)
                            {
                                // Gọi hàm OnError nếu có
                                if (_options.OnError != null)
                                    await _options.OnError(ex, message);

                                // Gửi qua DLQ nếu được cấu hình
                                if (!string.IsNullOrEmpty(_options.DeadLetterTopic))
                                {
                                    var dlqPublisher = new KafkaBentanikPublisher(new ProducerConfig
                                    {
                                        BootstrapServers = _options.BootstrapServers
                                    });
                                    await dlqPublisher.PublishAsync(_options.DeadLetterTopic, message);
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
                // graceful shutdown
            }
            catch (Exception ex)
            {
                // Optional logging
                Console.WriteLine($"[KafkaConsumer] Unexpected error: {ex.Message}");
            }
        }
    }
}
