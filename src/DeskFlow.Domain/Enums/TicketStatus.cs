namespace DeskFlow.Domain.Enums;

public enum TicketStatus
{
    New = 1,
    Triaged = 2,
    InProgress = 3,
    WaitingRequester = 4,
    WaitingThirdParty = 5,
    Resolved = 6,
    Closed = 7,
    Cancelled = 8
}
