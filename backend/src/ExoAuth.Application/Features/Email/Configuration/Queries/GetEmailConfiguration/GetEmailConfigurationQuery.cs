using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Configuration.Queries.GetEmailConfiguration;

/// <summary>
/// Query to get the email configuration.
/// </summary>
public sealed record GetEmailConfigurationQuery() : IQuery<EmailConfigurationDto>;
