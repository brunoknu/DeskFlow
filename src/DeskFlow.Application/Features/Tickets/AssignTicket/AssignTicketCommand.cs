namespace DeskFlow.Application.Features.Tickets.AssignTicket;

public sealed record AssignTicketCommand(
    Guid TicketId,
    Guid? AgentId,
    Guid ChangedByUserId,
    string? Reason,
    byte[] RowVersion);
