namespace DeskFlow.Application.Features.Tickets.GetTicketById;

public sealed record GetTicketByIdQuery(Guid TicketId, Guid RequestingUserId, bool IsPrivileged);
