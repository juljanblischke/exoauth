using Mediator;

namespace ExoAuth.Application.Features.Email.Dlq.Commands.DeleteDlqEmail;

/// <summary>
/// Command to delete an email from the DLQ (give up on sending).
/// </summary>
public sealed record DeleteDlqEmailCommand(Guid EmailLogId) : ICommand;
