namespace ExoAuth.Application.Features.Email.Models;

/// <summary>
/// Request to send a test email.
/// </summary>
public sealed record SendTestEmailRequest(
    string RecipientEmail,
    Guid? ProviderId = null
);

/// <summary>
/// Result of a test email attempt.
/// </summary>
public sealed record TestEmailResultDto(
    bool Success,
    string? Error,
    Guid? ProviderUsedId,
    string? ProviderUsedName,
    int AttemptCount,
    int TotalProvidersAttempted
);
