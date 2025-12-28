using ExoAuth.Application.Common.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data using Data Protection API.
/// </summary>
public sealed class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ExoAuth.MfaSecrets");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        return _protector.Protect(plainText);
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        return _protector.Unprotect(encryptedText);
    }
}
