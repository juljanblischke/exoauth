namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data using Data Protection API.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plaintext string.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    string Decrypt(string encryptedText);
}
