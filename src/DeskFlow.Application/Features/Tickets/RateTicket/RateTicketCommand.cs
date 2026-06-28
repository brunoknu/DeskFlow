namespace DeskFlow.Application.Features.Tickets.RateTicket;

public sealed record RateTicketCommand(
    Guid TicketId,
    Guid RequesterId,
    int Score,
    string? Comment);
