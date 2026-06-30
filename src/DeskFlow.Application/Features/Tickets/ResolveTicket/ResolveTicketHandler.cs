using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.ResolveTicket;

public class ResolveTicketHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _time;

    public ResolveTicketHandler(IApplicationDbContext db, IAuditLogger audit, TimeProvider time)
    {
        _db = db;
        _audit = audit;
        _time = time;
    }

    public async Task<Result> HandleAsync(ResolveTicketCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.ResolutionSummary))
            return Result.Failure("Resolution summary is required.");

        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == cmd.TicketId, ct);
        if (ticket is null) return Result.Failure("Ticket not found.");

        if (!ticket.RowVersion.SequenceEqual(cmd.RowVersion))
            return Result.Failure("Ticket was modified by another user. Please refresh and try again.");

        var now = _time.GetUtcNow();
        try
        {
            ticket.Resolve(cmd.ResolutionSummary, cmd.ChangedByUserId, now);

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                TicketId = ticket.Id,
                Number = ticket.Number,
                RequesterId = ticket.RequesterId
            });
            _db.OutboxMessages.Add(OutboxMessage.Create("TicketResolved", payload, now));

            await _db.SaveChangesAsync(ct);
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure("Ticket was modified by another user. Please refresh and try again.");
        }

        await _audit.LogAsync("TicketResolved", "Ticket", cmd.TicketId.ToString(), cmd.ChangedByUserId, ct: ct);
        return Result.Success();
    }
}
