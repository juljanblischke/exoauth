namespace ExoAuth.Domain.Enums;

/// <summary>
/// Represents the type/level of a user in the ExoAuth hierarchy.
/// </summary>
public enum UserType
{
    /// <summary>
    /// System-level administrator with platform-wide access.
    /// </summary>
    System = 1,

    /// <summary>
    /// Organization-level user managing a specific organization.
    /// </summary>
    Organization = 2,

    /// <summary>
    /// Project-level end-user within a specific project.
    /// </summary>
    Project = 3
}
