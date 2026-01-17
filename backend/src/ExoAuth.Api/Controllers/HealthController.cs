using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ExoAuth.Api.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// Detailed health check of all services
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var report = await _healthCheckService.CheckHealthAsync(ct);

        var response = new HealthResponse
        {
            Status = report.Status.ToString(),
            TotalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 2),
            Checks = report.Entries.Select(e => new HealthCheckResult
            {
                Name = e.Key,
                Status = e.Value.Status.ToString(),
                DurationMs = Math.Round(e.Value.Duration.TotalMilliseconds, 2),
                Error = e.Value.Exception?.Message
            }).ToList()
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }

    /// <summary>
    /// Simple liveness probe
    /// </summary>
    [HttpGet("live")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        return Ok(new { status = "Alive" });
    }

    /// <summary>
    /// Readiness probe - checks all dependencies
    /// </summary>
    [HttpGet("ready")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(CancellationToken ct)
    {
        var report = await _healthCheckService.CheckHealthAsync(ct);

        var response = new
        {
            status = report.Status == HealthStatus.Healthy ? "Ready" : "Not Ready",
            unhealthy = report.Entries
                .Where(e => e.Value.Status != HealthStatus.Healthy)
                .Select(e => e.Key)
                .ToList()
        };

        return report.Status == HealthStatus.Healthy
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}

public sealed class HealthResponse
{
    public string Status { get; init; } = string.Empty;
    public double TotalDurationMs { get; init; }
    public List<HealthCheckResult> Checks { get; init; } = new();
}

public sealed class HealthCheckResult
{
    public string Name { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public double DurationMs { get; init; }
    public string? Error { get; init; }
}
