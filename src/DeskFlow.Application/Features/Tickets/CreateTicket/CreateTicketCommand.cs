using DeskFlow.Domain.Enums;

namespace DeskFlow.Application.Features.Tickets.CreateTicket;

public sealed record CreateTicketCommand(
    string Title,
    string Description,
    Guid DepartmentId,
    Guid CategoryId,
    TicketPriority Priority,
    string? CriticalJustification,
    Guid RequesterId);
