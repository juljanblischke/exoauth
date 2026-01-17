using System.Text.Json;
using ExoAuth.Application.Common.Exceptions;
using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Features.Email.Models;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace ExoAuth.Application.Features.Email.Providers.Queries.GetEmailProvider;

public sealed class GetEmailProviderHandler : IQueryHandler<GetEmailProviderQuery, EmailProviderDetailDto>
{
    private readonly IAppDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;

    public GetEmailProviderHandler(IAppDbContext dbContext, IEncryptionService encryptionService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
    }

    public async ValueTask<EmailProviderDetailDto> Handle(GetEmailProviderQuery query, CancellationToken ct)
    {
        var provider = await _dbContext.EmailProviders
            .FirstOrDefaultAsync(x => x.Id == query.ProviderId, ct);

        if (provider is null)
        {
            throw new EmailProviderNotFoundException(query.ProviderId);
        }

        // Decrypt configuration
        var configJson = _encryptionService.Decrypt(provider.ConfigurationEncrypted);
        var config = JsonSerializer.Deserialize<EmailProviderConfigDto>(configJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new EmailProviderConfigDto();

        return new EmailProviderDetailDto(
            provider.Id,
            provider.Name,
            provider.Type,
            provider.Priority,
            provider.IsEnabled,
            config,
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
