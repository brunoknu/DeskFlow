using DeskFlow.Domain.Enums;

namespace DeskFlow.Application.Features.Tickets.ChangeTicketStatus;

public sealed record ChangeTicketStatusCommand(
    Guid TicketId,
    TicketStatus NewStatus,
    Guid ChangedByUserId,
    string? Reason,
    byte[] RowVersion);
