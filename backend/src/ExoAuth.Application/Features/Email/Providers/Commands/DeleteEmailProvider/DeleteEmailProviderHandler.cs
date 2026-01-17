using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Providers.Commands.DeleteEmailProvider;

public sealed class DeleteEmailProviderHandler : ICommandHandler<DeleteEmailProviderCommand>
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUser;

    public DeleteEmailProviderHandler(
        IAppDbContext dbContext,
        IAuditService auditService,
        ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _currentUser = currentUser;
    }

    public async ValueTask<Unit> Handle(DeleteEmailProviderCommand command, CancellationToken ct)
    {
        var provider = await _dbContext.EmailProviders
            .FirstOrDefaultAsync(x => x.Id == command.ProviderId, ct);

        if (provider is null)
        {
            throw new EmailProviderNotFoundException(command.ProviderId);
        }

        var providerName = provider.Name;
        var providerType = provider.Type;

        _dbContext.EmailProviders.Remove(provider);
        await _dbContext.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogWithContextAsync(
            AuditActions.EmailProviderDeleted,
            userId: _currentUser.UserId,
            targetUserId: null,
            entityType: "EmailProvider",
            entityId: command.ProviderId,
            details: new
            {
                Name = providerName,
                Type = providerType.ToString()
            },
            cancellationToken: ct);

        return Unit.Value;
    }
}
