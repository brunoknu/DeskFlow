namespace DeskFlow.Application.Features.Tickets.ResolveTicket;

public sealed record ResolveTicketCommand(
    Guid TicketId,
    string ResolutionSummary,
    Guid ChangedByUserId,
    byte[] RowVersion);
