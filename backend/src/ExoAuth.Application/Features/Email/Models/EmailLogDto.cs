using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Features.Email.Models;

/// <summary>
/// Email log list item DTO.
/// </summary>
public sealed record EmailLogDto(
    Guid Id,
    Guid? RecipientUserId,
    string RecipientEmail,
    string? RecipientUserFullName,
    string Subject,
    string TemplateName,
    string Language,
    EmailStatus Status,
    int RetryCount,
    string? LastError,
    Guid? SentViaProviderId,
    string? SentViaProviderName,
    DateTime QueuedAt,
    DateTime? SentAt,
    DateTime? FailedAt,
    DateTime? MovedToDlqAt,
    Guid? AnnouncementId,
    DateTime CreatedAt
);

/// <summary>
/// Email log detail DTO with template variables.
/// </summary>
public sealed record EmailLogDetailDto(
    Guid Id,
    Guid? RecipientUserId,
    string RecipientEmail,
    string? RecipientUserFullName,
    string Subject,
    string TemplateName,
    string? TemplateVariables,
    string Language,
    EmailStatus Status,
    int RetryCount,
    string? LastError,
    Guid? SentViaProviderId,
    string? SentViaProviderName,
    DateTime QueuedAt,
    DateTime? SentAt,
    DateTime? FailedAt,
    DateTime? MovedToDlqAt,
    Guid? AnnouncementId,
    DateTime CreatedAt
);

/// <summary>
/// Email log filter options.
/// </summary>
public sealed record EmailLogFiltersDto(
    List<string> Templates,
    List<EmailStatusFilterOption> Statuses
);

/// <summary>
/// Status filter option with display label.
/// </summary>
public sealed record EmailStatusFilterOption(
    EmailStatus Status,
    string Label
);
