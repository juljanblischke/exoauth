namespace ExoAuth.Domain.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime? UpdatedAt { get; protected set; }

    protected BaseEntity()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }

    protected void SetUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Regenerates the entity ID. Used for retry logic on extremely rare GUID collisions.
    /// </summary>
    public void RegenerateId()
    {
        Id = Guid.NewGuid();
    }
}
