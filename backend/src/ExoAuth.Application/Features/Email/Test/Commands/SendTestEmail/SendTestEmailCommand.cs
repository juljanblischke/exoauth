using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Test.Commands.SendTestEmail;

/// <summary>
/// Command to send a test email.
/// </summary>
public sealed record SendTestEmailCommand(
    string RecipientEmail,
    Guid? ProviderId = null
) : IRequest<TestEmailResultDto>;
