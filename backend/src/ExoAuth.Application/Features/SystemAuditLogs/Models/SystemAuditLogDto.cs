using System.Text.Json;

namespace ExoAuth.Application.Features.SystemAuditLogs.Models;

/// <summary>
/// System audit log entry for API responses.
/// </summary>
public sealed record SystemAuditLogDto(
    Guid Id,
    Guid? UserId,
    string? UserEmail,
    string? UserFullName,
    Guid? TargetUserId,
    string? TargetUserEmail,
    string? TargetUserFullName,
    string Action,
    string? EntityType,
    Guid? EntityId,
    string? IpAddress,
    string? UserAgent,
    JsonDocument? Details,
    DateTime CreatedAt
);

/// <summary>
/// Available filters for audit log queries.
/// </summary>
public sealed record AuditLogFiltersDto(
    IReadOnlyList<string> Actions,
    DateTime? EarliestDate,
    DateTime? LatestDate
);
