namespace DeskFlow.Application.Features.Tickets.ReopenTicket;

public sealed record ReopenTicketCommand(
    Guid TicketId,
    Guid RequestingUserId,
    byte[] RowVersion);
