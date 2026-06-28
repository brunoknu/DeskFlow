namespace DeskFlow.Application.Features.Tickets.AddInternalNote;

public sealed record AddInternalNoteCommand(
    Guid TicketId,
    Guid AuthorId,
    string Content);
