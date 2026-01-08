using System.Text.Json;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using ExoAuth.Domain.Entities;
using Mediator;

namespace ExoAuth.Application.Features.Email.Providers.Commands.CreateEmailProvider;

public sealed class CreateEmailProviderHandler : ICommandHandler<CreateEmailProviderCommand, EmailProviderDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;
    private readonly IAuditService _auditService;

    public CreateEmailProviderHandler(
        IAppDbContext dbContext,
        IEncryptionService encryptionService,
        IAuditService auditService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _auditService = auditService;
    }

    public async ValueTask<EmailProviderDto> Handle(CreateEmailProviderCommand command, CancellationToken ct)
    {
        // Serialize and encrypt configuration
        var configJson = JsonSerializer.Serialize(command.Configuration);
        var encryptedConfig = _encryptionService.Encrypt(configJson);

        // Create provider
        var provider = EmailProvider.Create(
            command.Name,
            command.Type,
            command.Priority,
            encryptedConfig,
            command.IsEnabled);

        _dbContext.EmailProviders.Add(provider);
        await _dbContext.SaveChangesAsync(ct);

        // Audit log
        await _auditService.LogAsync(
            "EMAIL_PROVIDER_CREATED",
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
