using DeskFlow.Domain.Enums;

namespace DeskFlow.Domain.Entities;

public class TicketCategory
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public TicketPriority DefaultPriority { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private TicketCategory() { }

    public static TicketCategory Create(string name, string? description, TicketPriority defaultPriority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var now = DateTimeOffset.UtcNow;
        return new TicketCategory
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            DefaultPriority = defaultPriority,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(string name, string? description, TicketPriority defaultPriority)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Description = description?.Trim();
        DefaultPriority = defaultPriority;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
