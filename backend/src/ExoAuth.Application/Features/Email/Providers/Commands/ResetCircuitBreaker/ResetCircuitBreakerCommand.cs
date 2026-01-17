using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Providers.Commands.ResetCircuitBreaker;

/// <summary>
/// Command to manually reset a provider's circuit breaker.
/// </summary>
public sealed record ResetCircuitBreakerCommand(Guid ProviderId) : ICommand<EmailProviderDto>;
