namespace DeskFlow.Application.Features.Tickets.CancelTicket;

public sealed record CancelTicketCommand(
    Guid TicketId,
    Guid RequestingUserId,
    bool IsPrivileged,
    string? Reason,
    byte[] RowVersion);
