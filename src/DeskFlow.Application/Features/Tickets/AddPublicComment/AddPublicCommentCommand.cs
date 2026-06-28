namespace DeskFlow.Application.Features.Tickets.AddPublicComment;

public sealed record AddPublicCommentCommand(
    Guid TicketId,
    Guid AuthorId,
    string Content,
    bool AuthorIsAgent);
