using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Providers.Commands.ReorderProviders;

/// <summary>
/// Command to reorder email provider priorities.
/// </summary>
public sealed record ReorderProvidersCommand(
    List<ProviderPriorityItem> Providers
) : ICommand<List<EmailProviderDto>>;
