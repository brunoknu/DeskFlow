using DeskFlow.Domain.Enums;

namespace DeskFlow.Domain.Entities;

public class TicketStatusHistory
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public TicketStatus PreviousStatus { get; private set; }
    public TicketStatus NewStatus { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public string? Reason { get; private set; }

    private TicketStatusHistory() { }

    public static TicketStatusHistory Create(
        Guid ticketId,
        TicketStatus previousStatus,
        TicketStatus newStatus,
        Guid changedByUserId,
        DateTimeOffset changedAtUtc,
        string? reason = null)
    {
        return new TicketStatusHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = changedAtUtc,
            Reason = reason?.Trim()
        };
    }
}
