using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Providers.Queries.GetEmailProviders;

public sealed class GetEmailProvidersHandler : IQueryHandler<GetEmailProvidersQuery, List<EmailProviderDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetEmailProvidersHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<List<EmailProviderDto>> Handle(GetEmailProvidersQuery query, CancellationToken ct)
    {
        var providers = await _dbContext.EmailProviders
            .OrderBy(x => x.Priority)
            .Select(x => new EmailProviderDto(
                x.Id,
                x.Name,
                x.Type,
                x.Priority,
                x.IsEnabled,
                x.FailureCount,
                x.LastFailureAt,
                x.CircuitBreakerOpenUntil,
                x.CircuitBreakerOpenUntil.HasValue && DateTime.UtcNow < x.CircuitBreakerOpenUntil,
                x.TotalSent,
                x.TotalFailed,
                x.TotalSent + x.TotalFailed == 0 ? 100.0 : (double)x.TotalSent / (x.TotalSent + x.TotalFailed) * 100.0,
                x.LastSuccessAt,
                x.CreatedAt,
                x.UpdatedAt))
            .ToListAsync(ct);

        return providers;
    }
}
