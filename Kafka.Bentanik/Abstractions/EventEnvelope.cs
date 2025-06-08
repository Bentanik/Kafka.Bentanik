namespace Kafka.Bentanik.Abstractions;

public class EventEnvelope
{
    public string EventTypeName { get; set; } = default!;
    public string Message { get; set; } = default!;

    public EventEnvelope() { }

    public EventEnvelope(Type type, string eventMessage)
        : this(type.Name!, eventMessage) { }

    private EventEnvelope(string eventTypeName, string eventMessage)
    {
        EventTypeName = eventTypeName ?? throw new ArgumentNullException(nameof(eventTypeName));
        Message = eventMessage ?? throw new ArgumentNullException(nameof(eventMessage));
    }
}
