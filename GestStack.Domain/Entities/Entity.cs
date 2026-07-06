namespace GestStack.Domain.Entities;

public abstract class Entity
{
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public abstract class Entity<TId> : Entity
{
    public TId Id { get; set; } = default!;
}
