using DeskFlow.Application.Common;
using DeskFlow.Domain.Exceptions;
using DeskFlow.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.RateTicket;

public class RateTicketHandler
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _time;

    public RateTicketHandler(IApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<Result> HandleAsync(RateTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Rating)
            .FirstOrDefaultAsync(t => t.Id == cmd.TicketId, ct);

        if (ticket is null || ticket.RequesterId != cmd.RequesterId)
            return Result.Failure("Chamado não encontrado.");

        var now = _time.GetUtcNow();
        try
        {
            ticket.AddRating(cmd.RequesterId, cmd.Score, cmd.Comment, now);
            await _db.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
    }
}
