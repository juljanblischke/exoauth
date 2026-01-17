namespace ExoAuth.Application.Common.Interfaces;

public interface IMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
    Task PublishAsync<T>(T message, string routingKey, CancellationToken ct = default) where T : class;
}
