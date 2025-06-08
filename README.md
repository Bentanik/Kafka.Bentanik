# Kafka.Bentanik

A lightweight Kafka library for .NET applications, supporting:

* Kafka Publisher with flexible configuration
* Kafka Subscriber with retry, DLQ and scoped DI
* Outbox pattern integration with plug-in DB support (MongoDB, PostgreSQL, MSSQL, etc.)

---

## Features

* ✅ Publish messages with retries, compression, idempotence, batching
* ✅ Background subscriber with scoped handler resolution
* ✅ Outbox Pattern for transactional publishing (supports any database)
* ✅ DLQ (Dead Letter Queue) support on subscriber and outbox
* ✅ Plug-in architecture: bring your own DB layer

---

## Installation

If published to NuGet:

```bash
dotnet add package Kafka.Bentanik
```

If using locally:

```bash
dotnet add package Kafka.Bentanik --source ./libs
```

---

## Kafka Publisher

### Register:

```csharp
builder.Services.AddKafkaPublisher(cfg =>
{
    cfg.BootstrapServers = "localhost:9092";
    cfg.Acks = Acks.All;
    cfg.EnableIdempotence = true;
    cfg.LingerMs = 100;
    cfg.CompressionType = CompressionType.Gzip;
});
```

### Use:

```csharp
public class MyService
{
    private readonly IKafkaBentanikPublisher _publisher;

    public MyService(IKafkaBentanikPublisher publisher)
        => _publisher = publisher;

    public async Task SendAsync()
    {
        await _publisher.PublishAsync("my-topic", new { Name = "Test", Created = DateTime.UtcNow });
    }
}
```

---

## Kafka Subscriber

### Define message + handler:

```csharp
public class MyMessage
{
    public string Id { get; set; }
}

public class MyHandler : IKafkaBentanikSubscriber<MyMessage>
{
    public Task HandleAsync(MyMessage message, CancellationToken ct)
    {
        Console.WriteLine($"Received message {message.Id}");
        return Task.CompletedTask;
    }
}
```

### Register subscriber:

```csharp
builder.Services.AddKafkaSubscriber<MyMessage, MyHandler>(opt =>
{
    opt.Topic = "my-topic";
    opt.GroupId = "my-group";
    opt.BootstrapServers = "localhost:9092";
    opt.MaxRetryAttempts = 3;
    opt.RetryDelay = TimeSpan.FromSeconds(2);
    opt.DeadLetterTopic = "my-topic-dlq";
});
```

---

## Outbox Pattern

Supports custom databases via `IOutboxStore` interface.

### OutboxMessage Model

```csharp
public class OutboxBentanikMessage
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
```

### Interface

```csharp
public interface IOutboxStore
{
    Task<List<OutboxMessage>> GetUnsentMessagesAsync(int maxCount, CancellationToken ct);
    Task MarkAsSentAsync(Guid messageId, CancellationToken ct);
    Task IncrementRetryAsync(Guid messageId, CancellationToken ct);
    Task MoveToDeadLetterAsync(Guid messageId, CancellationToken ct);
}
```

### Register Outbox:

```csharp
builder.Services.AddKafkaOutbox<MyOutboxStore>();
```

OutboxProcessor will run in background, check for unsent messages, publish them and update the store.

---

## Example Mongo Implementation

```csharp
public class MongoOutboxStore : IOutboxStore
{
    private readonly IMongoCollection<OutboxBentanikMessage> _collection;

    public MongoOutboxStore(IMongoDatabase db)
    {
        _collection = db.GetCollection<OutboxBentanikMessage>("outbox");
    }

    public Task<List<OutboxBentanikMessage>> GetUnsentMessagesAsync(int maxCount, CancellationToken ct)
        => _collection.Find(m => !m.Processed && m.Status != "DeadLettered")
                      .Limit(maxCount)
                      .ToListAsync(ct);

    public Task MarkAsSentAsync(string id, CancellationToken ct)
        => _collection.UpdateOneAsync(x => x.Id == id,
            Builders<OutboxBentanikMessage>.Update
                .Set(x => x.Processed, true)
                .Set(x => x.Status, "Success")
                .Set(x => x.ProcessedAt, DateTime.UtcNow),
            cancellationToken: ct);

    public Task IncrementRetryAsync(string id, CancellationToken ct)
        => _collection.UpdateOneAsync(x => x.Id == id,
            Builders<OutboxBentanikMessage>.Update.Inc(x => x.RetryCount, 1),
            cancellationToken: ct);

    public Task MoveToDeadLetterAsync(string id, CancellationToken ct)
        => _collection.UpdateOneAsync(x => x.Id == id,
            Builders<OutboxBentanikMessage>.Update
                .Set(x => x.Processed, true)
                .Set(x => x.Status, "DeadLettered")
                .Set(x => x.ProcessedAt, DateTime.UtcNow),
            cancellationToken: ct);
}
```

---

## License

MIT

---

## Roadmap (optional)

* [ ] Add exponential backoff
* [ ] Add metrics
* [ ] Add CLI to re-publish DLQ messages
* [ ] Add schema registry support

---

## Contributing

PRs welcome!
