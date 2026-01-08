using System.Text.Json;
using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Providers.Commands.UpdateEmailProvider;

public sealed class UpdateEmailProviderHandler : ICommandHandler<UpdateEmailProviderCommand, EmailProviderDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;

    public UpdateEmailProviderHandler(
        IAppDbContext dbContext,
        IEncryptionService encryptionService,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _auditService = auditService;
    }

    public async ValueTask<EmailProviderDto> Handle(UpdateEmailProviderCommand command, CancellationToken ct)
    {
        var provider = await _dbContext.EmailProviders
            .FirstOrDefaultAsync(x => x.Id == command.ProviderId, ct);

        if (provider is null)
        {
            throw new EmailProviderNotFoundException(command.ProviderId);
        }

        // Serialize and encrypt configuration
        var configJson = JsonSerializer.Serialize(command.Configuration);
        var encryptedConfig = _encryptionService.Encrypt(configJson);

        // Update provider
        provider.Update(
            command.Name,
            command.Type,
            command.Priority,
            encryptedConfig,
            command.IsEnabled);

        await _dbContext.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            "EMAIL_PROVIDER_UPDATED",
            userId: null,
            targetUserId: null,
            entityType: "EmailProvider",
            entityId: provider.Id,
            details: new
            {
                provider.Name,
                Type = command.Type.ToString(),
                provider.Priority,
                provider.IsEnabled
            },
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
