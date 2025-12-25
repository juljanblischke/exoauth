namespace ExoAuth.Application.Common.Interfaces;

/// <summary>
/// Service for hashing and verifying passwords using Argon2.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using Argon2.
    /// </summary>
    /// <param name="password">The plain text password to hash.</param>
    /// <returns>The hashed password.</returns>
    string Hash(string password);

    /// <summary>
    /// Verifies a password against a hash.
    /// </summary>
    /// <param name="password">The plain text password to verify.</param>
    /// <param name="hash">The hash to verify against.</param>
    /// <returns>True if the password matches the hash, otherwise false.</returns>
    bool Verify(string password, string hash);
}
