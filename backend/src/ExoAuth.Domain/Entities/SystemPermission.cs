namespace ExoAuth.Domain.Entities;

public sealed class SystemPermission : BaseEntity
{
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string Category { get; private set; } = null!;

    // Navigation properties
    private readonly List<SystemUserPermission> _userPermissions = new();
    public IReadOnlyCollection<SystemUserPermission> UserPermissions => _userPermissions.AsReadOnly();

    private SystemPermission() { } // EF Core

    public static SystemPermission Create(string name, string description, string category)
    {
        return new SystemPermission
        {
            Name = name,
            Description = description,
            Category = category
        };
    }

    public void Update(string? description = null, string? category = null)
    {
        if (description is not null)
            Description = description;

        if (category is not null)
            Category = category;

        SetUpdated();
    }
}
