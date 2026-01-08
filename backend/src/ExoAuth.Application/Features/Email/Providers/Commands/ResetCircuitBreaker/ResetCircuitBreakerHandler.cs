using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Providers.Commands.ResetCircuitBreaker;

public sealed class ResetCircuitBreakerHandler : ICommandHandler<ResetCircuitBreakerCommand, EmailProviderDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;

    public ResetCircuitBreakerHandler(IAppDbContext dbContext, IAuditService auditService)
    {
        _dbContext = dbContext;
        _auditService = auditService;
    }

    public async ValueTask<EmailProviderDto> Handle(ResetCircuitBreakerCommand command, CancellationToken ct)
    {
        var provider = await _dbContext.EmailProviders
            .FirstOrDefaultAsync(x => x.Id == command.ProviderId, ct);

        if (provider is null)
        {
            throw new EmailProviderNotFoundException(command.ProviderId);
        }

        provider.ResetCircuitBreaker();
        await _dbContext.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            "EMAIL_PROVIDER_CIRCUIT_BREAKER_RESET",
            userId: null,
            targetUserId: null,
            entityType: "EmailProvider",
            entityId: provider.Id,
            details: new { provider.Name },
            cancellationToken: ct);

        return new EmailProviderDto(
            provider.Id,
            provider.Name,
            provider.Type,
            provider.Priority,
            provider.IsEnabled,
            provider.FailureCount,
            provider.LastFailureAt,
            provider.CircuitBreakerOpenUntil,
            provider.IsCircuitBreakerOpen,
            provider.TotalSent,
            provider.TotalFailed,
            provider.SuccessRate,
            provider.LastSuccessAt,
            provider.CreatedAt,
            provider.UpdatedAt);
    }
}
