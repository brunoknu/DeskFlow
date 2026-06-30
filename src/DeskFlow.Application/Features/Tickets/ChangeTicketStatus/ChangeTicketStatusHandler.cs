using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.ChangeTicketStatus;

public class ChangeTicketStatusHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _time;

    public ChangeTicketStatusHandler(IApplicationDbContext db, IAuditLogger audit, TimeProvider time)
    {
        _db = db;
        _audit = audit;
        _time = time;
    }

    public async Task<Result> HandleAsync(ChangeTicketStatusCommand cmd, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == cmd.TicketId, ct);
        if (ticket is null)
            return Result.Failure("Ticket not found.");

        if (!ticket.RowVersion.SequenceEqual(cmd.RowVersion))
            return Result.Failure("Ticket was modified by another user. Please refresh and try again.");

        var now = _time.GetUtcNow();
        try
        {
            ticket.Transition(cmd.NewStatus, cmd.ChangedByUserId, now, cmd.Reason);

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                TicketId = ticket.Id,
                Number = ticket.Number,
                NewStatus = cmd.NewStatus.ToString()
            });
            _db.OutboxMessages.Add(OutboxMessage.Create($"TicketStatus_{cmd.NewStatus}", payload, now));

            await _db.SaveChangesAsync(ct);
        }
        catch (InvalidStatusTransitionException ex)
        {
            return Result.Failure(ex.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure("Ticket was modified by another user. Please refresh and try again.");
        }

        await _audit.LogAsync("TicketStatusChanged", "Ticket", cmd.TicketId.ToString(),
            cmd.ChangedByUserId, metadata: cmd.NewStatus.ToString(), ct: ct);

        return Result.Success();
    }
}
