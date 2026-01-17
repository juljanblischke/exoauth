using ExoAuth.Application.Common.Interfaces;
using ExoAuth.Application.Common.Models;
using ExoAuth.Domain.Entities;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ExoAuth.Infrastructure.Services;

/// <summary>
/// Service for WebAuthn/FIDO2 passkey operations using Fido2NetLib.
/// </summary>
public sealed class PasskeyService : IPasskeyService
{
    private readonly IFido2 _fido2;
    private readonly ICacheService _cache;
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<PasskeyService> _logger;

    private const int ChallengeTtlMinutes = 5;
    private const string ChallengeKeyPrefix = "passkey:challenge:";

    public PasskeyService(
        IFido2 fido2,
        ICacheService cache,
        IAppDbContext dbContext,
        ILogger<PasskeyService> logger)
    {
        _fido2 = fido2;
        _cache = cache;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(CredentialCreateOptions Options, string ChallengeId)> CreateRegistrationOptionsAsync(
        SystemUser user,
        IEnumerable<byte[]> existingCredentialIds,
        CancellationToken cancellationToken = default)
    {
        var fidoUser = new Fido2User
        {
            Id = user.Id.ToByteArray(),
            Name = user.Email,
            DisplayName = user.FullName
        };

        // Exclude existing credentials to prevent re-registration
        var excludeCredentials = existingCredentialIds
            .Select(id => new PublicKeyCredentialDescriptor(id))
            .ToList();

        var authenticatorSelection = new AuthenticatorSelection
        {
            RequireResidentKey = false,
            UserVerification = UserVerificationRequirement.Preferred
        };

        var options = _fido2.RequestNewCredential(
            fidoUser,
            excludeCredentials,
            authenticatorSelection,
            AttestationConveyancePreference.None);

        // Store challenge in Redis
        var challengeId = Guid.NewGuid().ToString("N");
        var cacheKey = $"{ChallengeKeyPrefix}{user.Id}:{challengeId}";

        await _cache.SetAsync(
            cacheKey,
            options.ToJson(),
            TimeSpan.FromMinutes(ChallengeTtlMinutes),
            cancellationToken);

        _logger.LogDebug("Created passkey registration options for user {UserId}, challenge {ChallengeId}",
            user.Id, challengeId);

        return (options, challengeId);
    }

    public async Task<PasskeyCredentialResult?> VerifyRegistrationAsync(
        Guid userId,
        string challengeId,
        AuthenticatorAttestationRawResponse attestationResponse,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ChallengeKeyPrefix}{userId}:{challengeId}";

        // Retrieve and delete challenge (one-time use)
        var optionsJson = await _cache.GetAsync<string>(cacheKey, cancellationToken);
        if (optionsJson is null)
        {
            _logger.LogWarning("Passkey registration challenge not found or expired for user {UserId}, challenge {ChallengeId}",
                userId, challengeId);
            return null;
        }

        // Delete immediately to ensure one-time use
        await _cache.RemoveAsync(cacheKey, cancellationToken);

        var options = CredentialCreateOptions.FromJson(optionsJson);

        try
        {
            var result = await _fido2.MakeNewCredentialAsync(
                attestationResponse,
                options,
                async (args, _) =>
                {
                    // Callback to check if credential is unique (doesn't already exist)
                    // Return true = credential IS unique, false = credential already exists
                    var existingPasskey = await _dbContext.Passkeys
                        .FirstOrDefaultAsync(p => p.CredentialId == args.CredentialId);
                    return existingPasskey is null; // true if unique (not found)
                });

            if (result.Result is null)
            {
                _logger.LogWarning("Passkey registration verification failed for user {UserId}: result is null",
                    userId);
                return null;
            }

            _logger.LogInformation("Passkey registered successfully for user {UserId}, credential {CredentialId}",
                userId, Convert.ToBase64String(result.Result.CredentialId));

            return new PasskeyCredentialResult(
                Id: result.Result.CredentialId,
                PublicKey: result.Result.PublicKey,
                Counter: result.Result.Counter,
                Type: result.Result.CredType ?? "public-key",
                AaGuid: result.Result.Aaguid
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Passkey registration verification failed for user {UserId}", userId);
            return null;
        }
    }

    public async Task<(AssertionOptions Options, string ChallengeId)> CreateLoginOptionsAsync(
        IEnumerable<byte[]>? allowedCredentialIds = null,
        CancellationToken cancellationToken = default)
    {
        var allowCredentials = allowedCredentialIds?
            .Select(id => new PublicKeyCredentialDescriptor(id))
            .ToList();

        var options = _fido2.GetAssertionOptions(
            allowCredentials ?? new List<PublicKeyCredentialDescriptor>(),
            UserVerificationRequirement.Preferred);

        // Store challenge in Redis (for login, we use a global prefix since user might not be known yet)
        var challengeId = Guid.NewGuid().ToString("N");
        var cacheKey = $"{ChallengeKeyPrefix}login:{challengeId}";

        await _cache.SetAsync(
            cacheKey,
            options.ToJson(),
            TimeSpan.FromMinutes(ChallengeTtlMinutes),
            cancellationToken);

        _logger.LogDebug("Created passkey login options, challenge {ChallengeId}", challengeId);

        return (options, challengeId);
    }

    public async Task<uint?> VerifyLoginAsync(
        string challengeId,
        AuthenticatorAssertionRawResponse assertionResponse,
        byte[] storedCredentialId,
        byte[] storedPublicKey,
        uint storedCounter,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{ChallengeKeyPrefix}login:{challengeId}";

        // Retrieve and delete challenge (one-time use)
        var optionsJson = await _cache.GetAsync<string>(cacheKey, cancellationToken);
        if (optionsJson is null)
        {
            _logger.LogWarning("Passkey login challenge not found or expired, challenge {ChallengeId}", challengeId);
            return null;
        }

        // Delete immediately to ensure one-time use
        await _cache.RemoveAsync(cacheKey, cancellationToken);

        var options = AssertionOptions.FromJson(optionsJson);

        try
        {
            var result = await _fido2.MakeAssertionAsync(
                assertionResponse,
                options,
                storedPublicKey,
                storedCounter,
                async (args, _) =>
                {
                    // Callback to verify the credential belongs to the expected user
                    // We verify by checking if the credential ID matches
                    return await Task.FromResult(args.CredentialId.SequenceEqual(storedCredentialId));
                });

            if (result.Status != "ok")
            {
                _logger.LogWarning("Passkey login verification failed: {Status}", result.Status);
                return null;
            }

            _logger.LogInformation("Passkey login verified successfully, counter updated from {OldCounter} to {NewCounter}",
                storedCounter, result.Counter);

            return result.Counter;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Passkey login verification failed");
            return null;
        }
    }
}
