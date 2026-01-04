using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Infrastructure.Caching;
using ExoAuth.Infrastructure.Messaging;
using ExoAuth.Infrastructure.Persistence;
using ExoAuth.Infrastructure.Persistence.Repositories;
using ExoAuth.Infrastructure.Persistence.Seeders;
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
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Redis
        services.AddSingleton<RedisConnectionFactory>();
        services.AddSingleton<ICacheService, RedisCacheService>();

        // RabbitMQ
        services.AddSingleton<RabbitMqConnectionFactory>();
        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        services.AddHostedService<RabbitMqBackgroundService>();
        // Note: SendEmailConsumer moved to ExoAuth.EmailWorker project

        // Core Services
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuditService, AuditService>();

        // Redis Services
        services.AddSingleton<IPermissionCacheService, PermissionCacheService>();
        services.AddSingleton<IBruteForceProtectionService, BruteForceProtectionService>();
        services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
        services.AddScoped<IForceReauthService, ForceReauthService>();
        services.AddSingleton<IRevokedSessionService, RevokedSessionService>();

        // Device Services
        services.AddSingleton<IGeoIpService, GeoIpService>();
        services.AddSingleton<IDeviceDetectionService, DeviceDetectionService>();

        // Email Services
        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
        services.AddScoped<IEmailService, EmailService>();

        // Password Reset
        services.AddScoped<IPasswordResetService, PasswordResetService>();

        // System Invite
        services.AddScoped<ISystemInviteService, SystemInviteService>();

        // Device Trust / Risk-Based Authentication
        services.AddScoped<ILoginPatternService, LoginPatternService>();
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        services.AddScoped<IDeviceService, DeviceService>();

        // Invite Cleanup
        services.AddScoped<IInviteCleanupService, InviteCleanupService>();
        services.AddHostedService<InviteCleanupBackgroundService>();

        // MFA Services
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddSingleton<IMfaService, MfaService>();
        services.AddSingleton<IBackupCodeService, BackupCodeService>();

        // Repositories
        services.AddScoped<ISystemUserRepository, SystemUserRepository>();

        // Seeders
        services.AddScoped<SystemPermissionSeeder>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
