using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Providers.Queries.GetEmailProviders;

/// <summary>
/// Query to get all email providers sorted by priority.
/// </summary>
public sealed record GetEmailProvidersQuery() : IQuery<List<EmailProviderDto>>;
