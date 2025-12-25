using System.Text.Json;

namespace ExoAuth.Domain.Entities;

public sealed class SystemAuditLog : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = null!;
    public string? EntityType { get; private set; }
    public Guid? EntityId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public JsonDocument? Details { get; private set; }

    // Navigation properties
    public SystemUser? User { get; private set; }

    private SystemAuditLog() { } // EF Core

    public static SystemAuditLog Create(
        string action,
        Guid? userId = null,
        string? entityType = null,
        Guid? entityId = null,
        string? ipAddress = null,
        string? userAgent = null,
        object? details = null)
    {
        return new SystemAuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details is not null
                ? JsonDocument.Parse(JsonSerializer.Serialize(details))
                : null
        };
    }
}
