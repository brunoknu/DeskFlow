using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Exceptions;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.ReopenTicket;

public class ReopenTicketHandler
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _time;

    public ReopenTicketHandler(ApplicationDbContext db, IAuditLogger audit, TimeProvider time)
    {
        _db = db;
        _audit = audit;
        _time = time;
    }

    public async Task<Result> HandleAsync(ReopenTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == cmd.TicketId, ct);
        if (ticket is null) return Result.Failure("Ticket not found.");

        // Only the requester or privileged users can reopen
        if (ticket.RequesterId != cmd.RequestingUserId)
            return Result.Failure("Ticket not found.");

        if (!ticket.RowVersion.SequenceEqual(cmd.RowVersion))
            return Result.Failure("Ticket was modified by another user. Please refresh and try again.");

        var now = _time.GetUtcNow();
        try
        {
            ticket.Reopen(cmd.RequestingUserId, now);

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                TicketId = ticket.Id,
                Number = ticket.Number
            });
            _db.OutboxMessages.Add(OutboxMessage.Create("TicketReopened", payload, now));

            await _db.SaveChangesAsync(ct);
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure("Ticket was modified by another user. Please refresh and try again.");
        }

        await _audit.LogAsync("TicketReopened", "Ticket", cmd.TicketId.ToString(), cmd.RequestingUserId, ct: ct);
        return Result.Success();
    }
}
