namespace Kafka.Bentanik.Services;

public class KafkaBentanikPublisher : IKafkaBentanikPublisher
{
    private readonly IProducer<string, string> _producer;

    public KafkaBentanikPublisher(ProducerConfig config)
    {
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        string json;
        if (message is string str)
        {
            json = str;
        }
        else
        {
            json = JsonSerializer.Serialize(message);
        }

        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = json
        }, cancellationToken);
    }
}
