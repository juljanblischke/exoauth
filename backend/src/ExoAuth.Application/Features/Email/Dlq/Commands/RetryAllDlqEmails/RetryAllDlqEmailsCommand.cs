using Mediator;

namespace ExoAuth.Application.Features.Email.Dlq.Commands.RetryAllDlqEmails;

/// <summary>
/// Command to retry all emails in the DLQ.
/// </summary>
public sealed record RetryAllDlqEmailsCommand() : ICommand<RetryAllDlqEmailsResult>;

/// <summary>
/// Result of retry all DLQ emails command.
/// </summary>
public sealed record RetryAllDlqEmailsResult(int Count);
