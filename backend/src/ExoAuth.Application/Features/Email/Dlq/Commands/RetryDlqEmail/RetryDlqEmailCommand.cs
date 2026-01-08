using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Dlq.Commands.RetryDlqEmail;

/// <summary>
/// Command to retry a single email from the DLQ.
/// </summary>
public sealed record RetryDlqEmailCommand(Guid EmailLogId) : ICommand<EmailLogDto>;
