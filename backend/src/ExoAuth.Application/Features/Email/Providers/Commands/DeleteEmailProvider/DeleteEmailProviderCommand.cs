using Mediator;

namespace ExoAuth.Application.Features.Email.Providers.Commands.DeleteEmailProvider;

/// <summary>
/// Command to delete an email provider.
/// </summary>
public sealed record DeleteEmailProviderCommand(Guid ProviderId) : ICommand;
