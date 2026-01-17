using ExoAuth.Application.Features.Email.Models;
using Mediator;

namespace ExoAuth.Application.Features.Email.Providers.Queries.GetEmailProvider;

/// <summary>
/// Query to get a single email provider with decrypted configuration.
/// </summary>
public sealed record GetEmailProviderQuery(Guid ProviderId) : IQuery<EmailProviderDetailDto>;
