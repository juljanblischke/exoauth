using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Messaging;

public sealed class RabbitMqBackgroundService : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqBackgroundService> _logger;

    public RabbitMqBackgroundService(
        RabbitMqConnectionFactory connectionFactory,
        ILogger<RabbitMqBackgroundService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RabbitMQ Background Service starting...");

        try
        {
            // Ensure connection is established
            await _connectionFactory.GetConnectionAsync(stoppingToken);
            _logger.LogInformation("RabbitMQ Background Service connected and ready");

            // Keep the service running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RabbitMQ Background Service stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ Background Service encountered an error");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ Background Service stopped");
        await base.StopAsync(cancellationToken);
    }
}
