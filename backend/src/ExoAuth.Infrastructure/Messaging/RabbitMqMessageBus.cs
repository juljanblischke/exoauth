using System.Text;
using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace ExoAuth.Infrastructure.Messaging;

public sealed class RabbitMqMessageBus : IMessageBus
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqMessageBus> _logger;

    private const string DefaultExchange = "exoauth.events";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RabbitMqMessageBus(RabbitMqConnectionFactory connectionFactory, ILogger<RabbitMqMessageBus> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        var routingKey = GetRoutingKey<T>();
        await PublishAsync(message, routingKey, ct);
    }

    public async Task PublishAsync<T>(T message, string routingKey, CancellationToken ct = default) where T : class
    {
        try
        {
            var channel = await _connectionFactory.CreateChannelAsync(ct);
            await using (channel)
            {
                await channel.ExchangeDeclareAsync(
                    exchange: DefaultExchange,
                    type: ExchangeType.Topic,
                    durable: true,
                    autoDelete: false,
                    cancellationToken: ct);

                var json = JsonSerializer.Serialize(message, JsonOptions);
                var body = Encoding.UTF8.GetBytes(json);

                var properties = new BasicProperties
                {
                    ContentType = "application/json",
                    DeliveryMode = DeliveryModes.Persistent,
                    MessageId = Guid.NewGuid().ToString(),
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                await channel.BasicPublishAsync(
                    exchange: DefaultExchange,
                    routingKey: routingKey,
                    mandatory: false,
                    basicProperties: properties,
                    body: body,
                    cancellationToken: ct);

                _logger.LogDebug("Published message to {Exchange} with routing key {RoutingKey}", DefaultExchange, routingKey);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to RabbitMQ");
            throw;
        }
    }

    private static string GetRoutingKey<T>()
    {
        var typeName = typeof(T).Name;
        return ToRoutingKey(typeName);
    }

    private static string ToRoutingKey(string typeName)
    {
        var result = new StringBuilder();
        for (var i = 0; i < typeName.Length; i++)
        {
            var c = typeName[i];
            if (char.IsUpper(c) && i > 0)
            {
                result.Append('.');
            }
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }
}
