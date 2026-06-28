using DeskFlow.Domain.Enums;

namespace DeskFlow.Domain.Entities;

public class SlaPolicy
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TicketPriority Priority { get; private set; }
    public int FirstResponseMinutes { get; private set; }
    public int ResolutionMinutes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private SlaPolicy() { }

    public static SlaPolicy Create(string name, TicketPriority priority, int firstResponseMinutes, int resolutionMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (firstResponseMinutes <= 0) throw new ArgumentException("First response minutes must be positive.", nameof(firstResponseMinutes));
        if (resolutionMinutes <= 0) throw new ArgumentException("Resolution minutes must be positive.", nameof(resolutionMinutes));
        if (firstResponseMinutes > resolutionMinutes)
            throw new ArgumentException("First response deadline cannot exceed resolution deadline.");

        var now = DateTimeOffset.UtcNow;
        return new SlaPolicy
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Priority = priority,
            FirstResponseMinutes = firstResponseMinutes,
            ResolutionMinutes = resolutionMinutes,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void Update(string name, int firstResponseMinutes, int resolutionMinutes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (firstResponseMinutes <= 0) throw new ArgumentException("First response minutes must be positive.", nameof(firstResponseMinutes));
        if (resolutionMinutes <= 0) throw new ArgumentException("Resolution minutes must be positive.", nameof(resolutionMinutes));
        if (firstResponseMinutes > resolutionMinutes)
            throw new ArgumentException("First response deadline cannot exceed resolution deadline.");

        Name = name.Trim();
        FirstResponseMinutes = firstResponseMinutes;
        ResolutionMinutes = resolutionMinutes;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public DateTimeOffset CalculateFirstResponseDue(DateTimeOffset from) =>
        from.AddMinutes(FirstResponseMinutes);

    public DateTimeOffset CalculateResolutionDue(DateTimeOffset from) =>
        from.AddMinutes(ResolutionMinutes);
}
