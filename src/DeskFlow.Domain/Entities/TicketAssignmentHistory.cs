namespace DeskFlow.Domain.Entities;

public class TicketAssignmentHistory
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid? PreviousAgentId { get; private set; }
    public Guid? NewAgentId { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public DateTimeOffset ChangedAtUtc { get; private set; }
    public string? Reason { get; private set; }

    private TicketAssignmentHistory() { }

    public static TicketAssignmentHistory Create(
        Guid ticketId,
        Guid? previousAgentId,
        Guid? newAgentId,
        Guid changedByUserId,
        DateTimeOffset changedAtUtc,
        string? reason = null)
    {
        return new TicketAssignmentHistory
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            PreviousAgentId = previousAgentId,
            NewAgentId = newAgentId,
            ChangedByUserId = changedByUserId,
            ChangedAtUtc = changedAtUtc,
            Reason = reason?.Trim()
        };
    }
}
