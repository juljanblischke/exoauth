using ExoAuth.Domain.Entities;

namespace ExoAuth.Application.Common.Interfaces;

public interface IEmailProviderFactory
{
    IEmailProviderImplementation CreateProvider(EmailProvider provider);
}
