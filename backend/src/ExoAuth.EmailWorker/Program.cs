using ExoAuth.EmailWorker;
using ExoAuth.EmailWorker.Consumers;
using ExoAuth.EmailWorker.Services;
using ExoAuth.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting ExoAuth Email Worker...");

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Configure Serilog
    builder.Services.AddSerilog((services, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"));

    // Add Data Protection (required for EncryptionService which decrypts email provider configs)
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/app/data/keys"))
        .SetApplicationName("ExoAuth");

    // Register Infrastructure services with worker context (skips web-specific services like AuditService, PasskeyService)
    builder.Services.AddInfrastructure(builder.Configuration, isWorkerContext: true);

    // Register EmailWorker-specific services
    builder.Services.AddSingleton<RabbitMqConnectionFactory>();
    builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

    // Register hosted services
    builder.Services.AddHostedService<EmailWorkerService>();
    builder.Services.AddHostedService<SendEmailConsumer>();

    var host = builder.Build();

    Log.Information("ExoAuth Email Worker configured successfully");

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ExoAuth Email Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
