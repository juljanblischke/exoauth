using StackExchange.Redis;

namespace ExoAuth.Infrastructure.Caching;

/// <summary>
/// Interface for Redis connection factory.
/// </summary>
public interface IRedisConnectionFactory
{
    /// <summary>
    /// Gets the Redis connection.
    /// </summary>
    Task<IConnectionMultiplexer> GetConnectionAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the Redis database.
    /// </summary>
    Task<IDatabase> GetDatabaseAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets whether the connection is established.
    /// </summary>
    bool IsConnected { get; }
}
