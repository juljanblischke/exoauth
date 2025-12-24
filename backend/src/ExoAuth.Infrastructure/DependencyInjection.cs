using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Infrastructure.Caching;
using ExoAuth.Infrastructure.Messaging;
using ExoAuth.Infrastructure.Persistence;
using ExoAuth.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExoAuth.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("Database"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });
        });

        // Redis
        services.AddSingleton<RedisConnectionFactory>();
        services.AddSingleton<ICacheService, RedisCacheService>();

        // RabbitMQ
        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        services.AddHostedService<RabbitMqBackgroundService>();

        // Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}
