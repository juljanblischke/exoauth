using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

public interface ICircuitBreakerService
{
    Task<bool> CanUseProviderAsync(EmailProvider provider, CancellationToken cancellationToken = default);
    Task RecordSuccessAsync(EmailProvider provider, CancellationToken cancellationToken = default);
    Task RecordFailureAsync(EmailProvider provider, CancellationToken cancellationToken = default);
    Task ResetCircuitBreakerAsync(EmailProvider provider, CancellationToken cancellationToken = default);
}
