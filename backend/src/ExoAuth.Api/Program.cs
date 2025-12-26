using ExoAuth.Api.Extensions;
using ExoAuth.Api.Middleware;
using ExoAuth.Api.Services;
using ExoAuth.Application;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Infrastructure;
using ExoAuth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

// Add services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Add layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add Mediator
builder.Services.AddMediator(options =>
{
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

// Add API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfiguration();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddHealthChecksConfiguration(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Configure middleware pipeline
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ExoAuth API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply pending migrations on startup
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (dbContext.Database.GetPendingMigrations().Any())
    {
        Log.Information("Applying pending database migrations...");
        await dbContext.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully");
    }
    else
    {
        Log.Information("Database is up to date - no migrations to apply");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "Failed to apply database migrations");
    throw; // Fail fast if migrations fail
}

// Invalidate permission cache on startup
try
{
    using var scope = app.Services.CreateScope();
    var cacheService = scope.ServiceProvider.GetService<ICacheService>();
    if (cacheService is not null)
    {
        await cacheService.DeleteByPatternAsync("user:permissions:*");
        Log.Information("Permission cache invalidated on startup");
    }
}
catch (Exception ex)
{
    Log.Warning(ex, "Failed to invalidate permission cache on startup - Redis may be unavailable");
}

// Log startup
Log.Information("ExoAuth API starting...");
Log.Information("Environment: {Environment}", app.Environment.EnvironmentName);

try
{
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Partial class to enable WebApplicationFactory access for integration tests
namespace ExoAuth.Api
{
    public partial class Program { }
}
