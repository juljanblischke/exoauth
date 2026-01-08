using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;

namespace ExoAuth.Application.Features.Email.Providers.Commands.UpdateEmailProvider;

/// <summary>
/// Command to update an existing email provider.
/// </summary>
public sealed record UpdateEmailProviderCommand(
    Guid ProviderId,
    string Name,
    EmailProviderType Type,
    int Priority,
    bool IsEnabled,
    EmailProviderConfigDto Configuration
) : ICommand<EmailProviderDto>;
