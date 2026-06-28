namespace DeskFlow.Domain.Entities;

public class Department
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private Department() { }

    public static Department Create(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var now = DateTimeOffset.UtcNow;
        return new Department
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(string name, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = description?.Trim();
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
