namespace ExoAuth.Application.Common.Models;

/// <summary>
/// Result from passkey registration verification.
/// </summary>
public sealed record PasskeyCredentialResult(
    byte[] Id,
    byte[] PublicKey,
    uint Counter,
    string Type,
    Guid AaGuid
);
