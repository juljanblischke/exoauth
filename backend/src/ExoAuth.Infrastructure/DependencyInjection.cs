using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Infrastructure.Caching;
using ExoAuth.Infrastructure.Messaging;
using ExoAuth.Infrastructure.Persistence;
using ExoAuth.Infrastructure.Persistence.Repositories;
using ExoAuth.Infrastructure.Persistence.Seeders;
using ExoAuth.Infrastructure.Services;
using ExoAuth.Infrastructure.Services.Captcha;
using ExoAuth.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExoAuth.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services for web applications (API).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        => AddInfrastructure(services, configuration, isWorkerContext: false);

    /// <summary>
    /// Adds infrastructure services with optional worker context support.
    /// Worker context skips services that require ASP.NET Core dependencies (IHttpContextAccessor, IFido2).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool isWorkerContext)
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
        services.AddSingleton<IRedisConnectionFactory>(sp => sp.GetRequiredService<RedisConnectionFactory>());
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

        // AuditService requires IHttpContextAccessor (only available in web context)
        if (!isWorkerContext)
        {
            services.AddScoped<IAuditService, AuditService>();
        }

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
        
        // Email Sending Infrastructure (Task 025)
        services.AddHttpClient("SendGrid");
        services.AddHttpClient("Mailgun");
        services.AddHttpClient("AmazonSES");
        services.AddHttpClient("Resend");
        services.AddHttpClient("Postmark");
        services.AddScoped<IEmailProviderFactory, EmailProviderFactory>();
        services.AddScoped<ICircuitBreakerService, CircuitBreakerService>();
        services.AddScoped<IEmailSendingService, EmailSendingService>();

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

        // Passkey Services (requires IFido2 - only available in web context)
        if (!isWorkerContext)
        {
            services.AddScoped<IPasskeyService, PasskeyService>();
        }

        // Rate Limiting Services
        services.Configure<RateLimitSettings>(configuration.GetSection("RateLimiting"));
        services.AddSingleton<IRateLimitService, RateLimitService>();
        services.AddScoped<IIpRestrictionService, IpRestrictionService>();

        // CAPTCHA Services
        services.Configure<CaptchaSettings>(configuration.GetSection("Captcha"));
        services.AddHttpClient<TurnstileProvider>();
        services.AddHttpClient<RecaptchaProvider>();
        services.AddHttpClient<HCaptchaProvider>();
        services.AddSingleton<DisabledCaptchaProvider>();
        services.AddSingleton<ICaptchaProvider>(sp =>
        {
            var settings = configuration.GetSection("Captcha").Get<CaptchaSettings>() ?? new CaptchaSettings();
            if (!settings.Enabled)
            {
                return sp.GetRequiredService<DisabledCaptchaProvider>();
            }

            return settings.Provider.ToLowerInvariant() switch
            {
                "turnstile" => sp.GetRequiredService<TurnstileProvider>(),
                "recaptcha" => sp.GetRequiredService<RecaptchaProvider>(),
                "hcaptcha" => sp.GetRequiredService<HCaptchaProvider>(),
                _ => sp.GetRequiredService<DisabledCaptchaProvider>()
            };
        });
        services.AddSingleton<ICaptchaService, CaptchaService>();

        // Repositories
        services.AddScoped<ISystemUserRepository, SystemUserRepository>();

        // Seeders
        services.AddScoped<SystemPermissionSeeder>();
        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
