using ExoAuth.Domain.Enums;

namespace ExoAuth.Application.Features.Email.Models;

/// <summary>
/// Email provider list item and detail DTO.
/// </summary>
public sealed record EmailProviderDto(
    Guid Id,
    string Name,
    EmailProviderType Type,
    int Priority,
    bool IsEnabled,
    int FailureCount,
    DateTime? LastFailureAt,
    DateTime? CircuitBreakerOpenUntil,
    bool IsCircuitBreakerOpen,
    int TotalSent,
    int TotalFailed,
    double SuccessRate,
    DateTime? LastSuccessAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>
/// Email provider with decrypted configuration for editing.
/// </summary>
public sealed record EmailProviderDetailDto(
    Guid Id,
    string Name,
    EmailProviderType Type,
    int Priority,
    bool IsEnabled,
    EmailProviderConfigDto Configuration,
    int FailureCount,
    DateTime? LastFailureAt,
    DateTime? CircuitBreakerOpenUntil,
    bool IsCircuitBreakerOpen,
    int TotalSent,
    int TotalFailed,
    double SuccessRate,
    DateTime? LastSuccessAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

/// <summary>
/// Request to create a new email provider.
/// </summary>
public sealed record CreateEmailProviderRequest(
    string Name,
    EmailProviderType Type,
    int Priority,
    bool IsEnabled,
    EmailProviderConfigDto Configuration
);

/// <summary>
/// Request to update an email provider.
/// </summary>
public sealed record UpdateEmailProviderRequest(
    string Name,
    EmailProviderType Type,
    int Priority,
    bool IsEnabled,
    EmailProviderConfigDto Configuration
);

/// <summary>
/// Request to reorder provider priorities.
/// </summary>
public sealed record ReorderProvidersRequest(
    List<ProviderPriorityItem> Providers
);

/// <summary>
/// Item for reordering providers.
/// </summary>
public sealed record ProviderPriorityItem(
    Guid ProviderId,
    int Priority
);

/// <summary>
/// Base configuration DTO with common fields.
/// </summary>
public sealed record EmailProviderConfigDto
{
    public string FromEmail { get; init; } = null!;
    public string FromName { get; init; } = null!;

    // SMTP specific
    public string? Host { get; init; }
    public int? Port { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool? UseSsl { get; init; }

    // API providers (SendGrid, Resend, Postmark)
    public string? ApiKey { get; init; }

    // Mailgun specific
    public string? Domain { get; init; }
    public string? Region { get; init; } // "EU" or "US"

    // Amazon SES specific
    public string? AccessKey { get; init; }
    public string? SecretKey { get; init; }
    public string? AwsRegion { get; init; }

    // Postmark specific
    public string? ServerToken { get; init; }
}
