using RabbitMQ.Client;

namespace ExoAuth.EmailWorker;

public sealed class RabbitMqConnectionFactory : IAsyncDisposable
{
    private readonly ILogger<RabbitMqConnectionFactory> _logger;
    private readonly ConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqConnectionFactory(IConfiguration configuration, ILogger<RabbitMqConnectionFactory> logger)
    {
        _logger = logger;

        var connectionString = configuration.GetConnectionString("RabbitMq")
            ?? throw new InvalidOperationException("RabbitMQ connection string is not configured");

        _connectionFactory = new ConnectionFactory
        {
            Uri = new Uri(connectionString),
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<IConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        await _connectionLock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _logger.LogInformation("Connecting to RabbitMQ...");

            _connection = await _connectionFactory.CreateConnectionAsync(ct);

            _logger.LogInformation("Connected to RabbitMQ successfully");

            return _connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to RabbitMQ");
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task<IChannel> CreateChannelAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        return await connection.CreateChannelAsync(cancellationToken: ct);
    }

    public bool IsConnected => _connection?.IsOpen ?? false;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }

        _connectionLock.Dispose();
        _disposed = true;
    }
}
