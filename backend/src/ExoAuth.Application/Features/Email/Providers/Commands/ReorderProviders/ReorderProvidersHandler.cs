using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Providers.Commands.ReorderProviders;

public sealed class ReorderProvidersHandler : ICommandHandler<ReorderProvidersCommand, List<EmailProviderDto>>
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;

    public ReorderProvidersHandler(IAppDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async ValueTask<List<EmailProviderDto>> Handle(ReorderProvidersCommand command, CancellationToken ct)
    {
        var providerIds = command.Providers.Select(p => p.ProviderId).ToList();
        var providers = await _dbContext.EmailProviders
            .Where(x => providerIds.Contains(x.Id))
            .ToListAsync(ct);

        // Verify all providers exist
        foreach (var item in command.Providers)
        {
            var provider = providers.FirstOrDefault(p => p.Id == item.ProviderId);
            if (provider is null)
            {
                throw new EmailProviderNotFoundException(item.ProviderId);
            }

            provider.SetPriority(item.Priority);
        }

        await _dbContext.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            "EMAIL_PROVIDERS_REORDERED",
            userId: null,
            targetUserId: null,
            entityType: "EmailProvider",
            entityId: null,
            details: new
            {
                Providers = command.Providers.Select(p => new { p.ProviderId, p.Priority })
            },
            cancellationToken: ct);

        // Return updated list sorted by priority
        return providers
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
                x.IsCircuitBreakerOpen,
                x.TotalSent,
                x.TotalFailed,
                x.SuccessRate,
                x.LastSuccessAt,
                x.CreatedAt,
                x.UpdatedAt))
            .ToList();
    }
}
