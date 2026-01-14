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
    private readonly ICurrentUserService _currentUser;

    public ResetCircuitBreakerHandler(
        IAppDbContext dbContext,
        IAuditService auditService,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _currentUser = currentUser;
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
        await _auditService.LogWithContextAsync(
            AuditActions.EmailProviderCircuitBreakerReset,
            userId: _currentUser.UserId,
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
