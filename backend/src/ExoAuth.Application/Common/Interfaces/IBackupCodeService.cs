namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for generating and validating MFA backup codes.
/// </summary>
public interface IBackupCodeService
{
    /// <summary>
    /// Generates a set of backup codes in XXXX-XXXX format.
    /// </summary>
    /// <param name="count">Number of codes to generate (default: 10)</param>
    /// <returns>List of plaintext backup codes</returns>
    List<string> GenerateCodes(int count = 10);

    /// <summary>
    /// Hashes a backup code for storage.
    /// </summary>
    /// <param name="code">Plaintext backup code</param>
    /// <returns>Hashed backup code</returns>
    string HashCode(string code);

    /// <summary>
    /// Verifies a backup code against a hash.
    /// </summary>
    /// <param name="code">Plaintext backup code (with or without hyphen)</param>
    /// <param name="hash">Stored hash</param>
    /// <returns>True if code matches hash</returns>
    bool VerifyCode(string code, string hash);

    /// <summary>
    /// Normalizes a backup code (removes hyphens, converts to uppercase).
    /// </summary>
    /// <param name="code">Input code</param>
    /// <returns>Normalized code</returns>
    string NormalizeCode(string code);
}
