using ExoAuth.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class InviteCleanupBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InviteCleanupBackgroundService> _logger;
    private readonly bool _enabled;
    private readonly int _retentionDays;
    private readonly TimeSpan _runInterval;

    public InviteCleanupBackgroundService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<InviteCleanupBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        _enabled = configuration.GetValue<bool>("InviteCleanup:Enabled", true);
        _retentionDays = configuration.GetValue<int>("InviteCleanup:RetentionDays", 30);
        var runIntervalHours = configuration.GetValue<int>("InviteCleanup:RunIntervalHours", 24);
        _runInterval = TimeSpan.FromHours(runIntervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Invite cleanup background service is disabled");
            return;
        }

        _logger.LogInformation(
            "Invite cleanup background service started. Running every {Hours} hours, retention: {Days} days",
            _runInterval.TotalHours,
            _retentionDays);

        // Wait a bit before first run to let the app fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during invite cleanup");
            }

            try
            {
                await Task.Delay(_runInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Invite cleanup background service stopped");
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IInviteCleanupService>();

        var deletedCount = await cleanupService.CleanupExpiredAndRevokedInvitesAsync(_retentionDays, cancellationToken);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Invite cleanup completed. Deleted {Count} old invites", deletedCount);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Invite cleanup background service stopping...");
        await base.StopAsync(cancellationToken);
    }
}
