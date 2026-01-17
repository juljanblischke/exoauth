using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Logs.Queries.GetEmailLog;

/// <summary>
/// Query to get a specific email log by ID.
/// </summary>
public sealed record GetEmailLogQuery(Guid Id) : IQuery<EmailLogDetailDto>;
