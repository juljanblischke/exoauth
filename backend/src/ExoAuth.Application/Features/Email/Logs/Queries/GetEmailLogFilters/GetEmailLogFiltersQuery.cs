using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Logs.Queries.GetEmailLogFilters;

/// <summary>
/// Query to get available filter options for email logs.
/// </summary>
public sealed record GetEmailLogFiltersQuery() : IQuery<EmailLogFiltersDto>;
