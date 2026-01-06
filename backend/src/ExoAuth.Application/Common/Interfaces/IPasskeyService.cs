using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using Fido2NetLib;
using Fido2NetLib.Objects;

namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for WebAuthn/FIDO2 passkey operations.
/// </summary>
public interface IPasskeyService
{
    /// <summary>
    /// Creates registration options for a new passkey.
    /// </summary>
    /// <param name="user">The user registering a passkey.</param>
    /// <param name="existingCredentialIds">Existing credential IDs to exclude.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registration options and challenge ID for verification.</returns>
    Task<(CredentialCreateOptions Options, string ChallengeId)> CreateRegistrationOptionsAsync(
        SystemUser user,
        IEnumerable<byte[]> existingCredentialIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the registration response and extracts credential data.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="challengeId">The challenge ID from registration options.</param>
    /// <param name="attestationResponse">The attestation response from the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verified credential result or null if verification failed.</returns>
    Task<PasskeyCredentialResult?> VerifyRegistrationAsync(
        Guid userId,
        string challengeId,
        AuthenticatorAttestationRawResponse attestationResponse,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates login options for passkey authentication.
    /// </summary>
    /// <param name="allowedCredentialIds">Allowed credential IDs for this user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Assertion options and challenge ID for verification.</returns>
    Task<(AssertionOptions Options, string ChallengeId)> CreateLoginOptionsAsync(
        IEnumerable<byte[]>? allowedCredentialIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies the login assertion response.
    /// </summary>
    /// <param name="challengeId">The challenge ID from login options.</param>
    /// <param name="assertionResponse">The assertion response from the client.</param>
    /// <param name="storedCredentialId">The stored credential ID.</param>
    /// <param name="storedPublicKey">The stored public key.</param>
    /// <param name="storedCounter">The stored signature counter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new counter value if verification succeeded, null otherwise.</returns>
    Task<uint?> VerifyLoginAsync(
        string challengeId,
        AuthenticatorAssertionRawResponse assertionResponse,
        byte[] storedCredentialId,
        byte[] storedPublicKey,
        uint storedCounter,
        CancellationToken cancellationToken = default);
}
