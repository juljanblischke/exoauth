using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Features.Email.Models;

/// <summary>
/// Email announcement list item DTO.
/// </summary>
public sealed record EmailAnnouncementDto(
    Guid Id,
    string Subject,
    EmailAnnouncementTarget TargetType,
    string? TargetPermission,
    int TotalRecipients,
    int SentCount,
    int FailedCount,
    double Progress,
    EmailAnnouncementStatus Status,
    Guid CreatedByUserId,
    string? CreatedByUserFullName,
    DateTime? SentAt,
    DateTime CreatedAt
);

/// <summary>
/// Email announcement detail DTO.
/// </summary>
public sealed record EmailAnnouncementDetailDto(
    Guid Id,
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    EmailAnnouncementTarget TargetType,
    string? TargetPermission,
    List<Guid>? TargetUserIds,
    int TotalRecipients,
    int SentCount,
    int FailedCount,
    double Progress,
    EmailAnnouncementStatus Status,
    Guid CreatedByUserId,
    string? CreatedByUserFullName,
    DateTime? SentAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>
/// Request to create an announcement.
/// </summary>
public sealed record CreateAnnouncementRequest(
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    EmailAnnouncementTarget TargetType,
    string? TargetPermission,
    List<Guid>? TargetUserIds
);

/// <summary>
/// Request to update an announcement.
/// </summary>
public sealed record UpdateAnnouncementRequest(
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    EmailAnnouncementTarget TargetType,
    string? TargetPermission,
    List<Guid>? TargetUserIds
);

/// <summary>
/// Request to preview an announcement email.
/// </summary>
public sealed record PreviewAnnouncementRequest(
    string Subject,
    string HtmlBody,
    string? PlainTextBody
);

/// <summary>
/// Announcement preview result.
/// </summary>
public sealed record AnnouncementPreviewDto(
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    int EstimatedRecipients
);
