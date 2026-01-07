using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace ExoAuth.Infrastructure.Caching;

public sealed class RedisConnectionFactory : IRedisConnectionFactory, IDisposable
{
    private readonly ILogger<RedisConnectionFactory> _logger;
    private readonly string _connectionString;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private ConnectionMultiplexer? _connection;
    private bool _disposed;

    public RedisConnectionFactory(IConfiguration configuration, ILogger<RedisConnectionFactory> logger)
    {
        _logger = logger;
        _connectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Redis connection string is not configured");
    }

    public async Task<IConnectionMultiplexer> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is { IsConnected: true })
            return _connection;

        await _connectionLock.WaitAsync(ct);
        try
        {
            if (_connection is { IsConnected: true })
                return _connection;

            _logger.LogInformation("Connecting to Redis...");

            var options = ConfigurationOptions.Parse(_connectionString);
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 3;
            options.ConnectTimeout = 5000;

            _connection = await ConnectionMultiplexer.ConnectAsync(options);

            _logger.LogInformation("Connected to Redis successfully");

            return _connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Redis");
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<IDatabase> GetDatabaseAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        return connection.GetDatabase();
    }

    public bool IsConnected => _connection?.IsConnected ?? false;

    public void Dispose()
    {
        if (_disposed)
            return;

        _connection?.Dispose();
        _connectionLock.Dispose();
        _disposed = true;
    }
}
