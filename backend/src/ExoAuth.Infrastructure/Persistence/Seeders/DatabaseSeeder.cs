using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Persistence.Seeders;

public sealed class DatabaseSeeder
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        _logger.LogInformation("Starting database seeding...");

        // Apply pending migrations
        await context.Database.MigrateAsync(cancellationToken);
        _logger.LogInformation("Database migrations applied");

        // Seed system permissions
        var permissionSeeder = scope.ServiceProvider.GetRequiredService<SystemPermissionSeeder>();
        await permissionSeeder.SeedAsync(cancellationToken);

        _logger.LogInformation("Database seeding completed");
    }
}
