using DeskFlow.Domain.Enums;

namespace DeskFlow.Application.Features.Tickets.SearchTickets;

public sealed record SearchTicketsQuery(
    Guid RequestingUserId,
    bool IsPrivileged,
    TicketStatus? Status = null,
    TicketPriority? Priority = null,
    Guid? AssignedAgentId = null,
    Guid? CategoryId = null,
    Guid? DepartmentId = null,
    string? Search = null,
    bool? IsOverdue = null,
    string SortBy = "CreatedAtUtc",
    bool SortDescending = true,
    int Page = 1,
    int PageSize = 20);
