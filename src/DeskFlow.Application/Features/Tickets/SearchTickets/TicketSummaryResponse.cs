using DeskFlow.Domain.Enums;

namespace DeskFlow.Application.Features.Tickets.SearchTickets;

public sealed record TicketSummaryResponse(
    Guid Id,
    string Number,
    string Title,
    TicketPriority Priority,
    TicketStatus Status,
    string RequesterName,
    string? AssignedAgentName,
    string CategoryName,
    string DepartmentName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ResolutionDueAtUtc,
    bool IsFirstResponseBreached,
    bool IsResolutionBreached);
