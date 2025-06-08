using Kafka.Bentanik.Workers;
namespace Kafka.Bentanik.Extensions;
public static class KafkaServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaBentanikPublisher(this IServiceCollection services, Action<ProducerConfig> configure)
    {
        var config = new ProducerConfig();
        configure(config);
        services.AddSingleton<IKafkaBentanikPublisher>(new KafkaBentanikPublisher(config));
        return services;
    }

    public static IServiceCollection AddKafkaBentanikSubscriber<TMessage, THandler>(
        this IServiceCollection services,
        Action<KafkaBentanikSubscriberOptions<TMessage>> configureOptions)
        where THandler : class, IKafkaBentanikSubscriber<TMessage>
    {
        services.Configure(configureOptions);
        services.AddScoped<IKafkaBentanikSubscriber<TMessage>, THandler>();
        services.AddHostedService<KafkaBentanikSubscriber<TMessage>>();
        return services;
    }

    public static IServiceCollection AddKafkaBentanikOutbox<TStore>(this IServiceCollection services)
        where TStore : class, IOutboxBentanikStore
    {
        services.AddScoped<IOutboxBentanikStore, TStore>();
        services.AddHostedService<OutboxBentanikProcessor>();
        return services;
    }
}
