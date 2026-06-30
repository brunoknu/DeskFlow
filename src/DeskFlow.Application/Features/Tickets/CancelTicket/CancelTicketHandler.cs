using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Enums;
using DeskFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.CancelTicket;

public class CancelTicketHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _time;

    public CancelTicketHandler(IApplicationDbContext db, IAuditLogger audit, TimeProvider time)
    {
        _db = db;
        _audit = audit;
        _time = time;
    }

    public async Task<Result> HandleAsync(CancelTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == cmd.TicketId, ct);
        if (ticket is null) return Result.Failure("Ticket not found.");

        if (!cmd.IsPrivileged)
        {
            if (ticket.RequesterId != cmd.RequestingUserId)
                return Result.Failure("Ticket not found.");

            // Requester can only cancel tickets not yet in progress
            if (ticket.Status is not (TicketStatus.New or TicketStatus.Triaged))
                return Result.Failure("Ticket cannot be cancelled once it is in progress.");
        }

        if (!ticket.RowVersion.SequenceEqual(cmd.RowVersion))
            return Result.Failure("Ticket was modified by another user. Please refresh and try again.");

        var now = _time.GetUtcNow();
        try
        {
            ticket.Cancel(cmd.RequestingUserId, now, cmd.Reason);
            await _db.SaveChangesAsync(ct);
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure("Ticket was modified by another user. Please refresh and try again.");
        }

        await _audit.LogAsync("TicketCancelled", "Ticket", cmd.TicketId.ToString(), cmd.RequestingUserId, ct: ct);
        return Result.Success();
    }
}
