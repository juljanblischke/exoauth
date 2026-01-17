namespace ExoAuth.EmailWorker;

public sealed class EmailWorkerService : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<EmailWorkerService> _logger;

    public EmailWorkerService(
        RabbitMqConnectionFactory connectionFactory,
        ILogger<EmailWorkerService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email Worker Service starting...");

        try
        {
            // Ensure RabbitMQ connection is established
            await _connectionFactory.GetConnectionAsync(stoppingToken);
            _logger.LogInformation("Email Worker Service connected to RabbitMQ and ready");

            // Keep the service running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Email Worker Service stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email Worker Service encountered an error");
            throw;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Email Worker Service stopped");
        await base.StopAsync(cancellationToken);
    }
}
