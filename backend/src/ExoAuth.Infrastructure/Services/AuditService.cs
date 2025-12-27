using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Domain.Entities;
using ExoAuth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        AppDbContext context,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        Guid? userId = null,
        Guid? targetUserId = null,
        string? entityType = null,
        Guid? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = SystemAuditLog.Create(
                action: action,
                userId: userId,
                targetUserId: targetUserId,
                entityType: entityType,
                entityId: entityId,
                details: details
            );

            await _context.SystemAuditLogs.AddAsync(auditLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Audit log created: {Action} by {UserId} targeting {TargetUserId}", action, userId, targetUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for action {Action}", action);
            // Don't throw - audit logging should not break the main flow
        }
    }

    public async Task LogWithContextAsync(
        string action,
        Guid? userId = null,
        Guid? targetUserId = null,
        string? entityType = null,
        Guid? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;

            string? ipAddress = null;
            string? userAgent = null;

            if (httpContext is not null)
            {
                ipAddress = GetClientIpAddress(httpContext);
                userAgent = httpContext.Request.Headers["User-Agent"].ToString();

                // Truncate user agent if too long
                if (userAgent?.Length > 500)
                    userAgent = userAgent[..500];
            }

            var auditLog = SystemAuditLog.Create(
                action: action,
                userId: userId,
                targetUserId: targetUserId,
                entityType: entityType,
                entityId: entityId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                details: details
            );

            await _context.SystemAuditLogs.AddAsync(auditLog, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Audit log created: {Action} by {UserId} targeting {TargetUserId} from {IpAddress}", action, userId, targetUserId, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create audit log for action {Action}", action);
            // Don't throw - audit logging should not break the main flow
        }
    }

    private static string? GetClientIpAddress(HttpContext context)
    {
        // Check for forwarded headers first (proxy/load balancer)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            // Take the first IP in the chain (original client)
            var ip = forwardedFor.Split(',')[0].Trim();
            return ip;
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
