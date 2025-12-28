namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for MFA (Multi-Factor Authentication) operations using TOTP.
/// </summary>
public interface IMfaService
{
    /// <summary>
    /// Generates a new MFA secret key.
    /// </summary>
    /// <returns>Base32 encoded secret key</returns>
    string GenerateSecret();

    /// <summary>
    /// Generates a QR code URL for authenticator apps.
    /// </summary>
    /// <param name="email">User's email for the account label</param>
    /// <param name="secret">Base32 encoded secret key</param>
    /// <param name="issuer">Application name (e.g., "ExoAuth")</param>
    /// <returns>otpauth:// URI for QR code generation</returns>
    string GenerateQrCodeUri(string email, string secret, string issuer);

    /// <summary>
    /// Validates a TOTP code against the secret.
    /// </summary>
    /// <param name="secret">Base32 encoded secret key</param>
    /// <param name="code">6-digit TOTP code</param>
    /// <returns>True if code is valid</returns>
    bool ValidateCode(string secret, string code);

    /// <summary>
    /// Generates a temporary MFA token for the login flow.
    /// This token is used between password verification and MFA verification.
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="sessionId">Session ID for the pending login</param>
    /// <returns>JWT-like token valid for MFA verification</returns>
    string GenerateMfaToken(Guid userId, Guid? sessionId);

    /// <summary>
    /// Validates an MFA token and extracts the user ID.
    /// </summary>
    /// <param name="token">MFA token</param>
    /// <returns>User ID and Session ID if valid, null if invalid or expired</returns>
    (Guid userId, Guid? sessionId)? ValidateMfaToken(string token);
}
