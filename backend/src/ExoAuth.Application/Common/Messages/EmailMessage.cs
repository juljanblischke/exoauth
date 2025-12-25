namespace ExoAuth.Application.Common.Messages;

/// <summary>
/// Message for sending an email via the message queue.
/// </summary>
public sealed record SendEmailMessage(
    string To,
    string Subject,
    string TemplateName,
    string Language,
    Dictionary<string, string> Variables
);
