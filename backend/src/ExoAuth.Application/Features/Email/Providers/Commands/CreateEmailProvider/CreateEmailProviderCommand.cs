using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Enums;
using Mediator;

namespace ExoAuth.Application.Features.Email.Providers.Commands.CreateEmailProvider;

/// <summary>
/// Command to create a new email provider.
/// </summary>
public sealed record CreateEmailProviderCommand(
    string Name,
    EmailProviderType Type,
    int Priority,
    bool IsEnabled,
    EmailProviderConfigDto Configuration
) : ICommand<EmailProviderDto>;
