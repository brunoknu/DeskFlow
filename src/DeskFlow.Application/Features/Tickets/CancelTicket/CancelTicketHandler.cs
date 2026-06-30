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
        if (ticket is null) return Result.Failure("Chamado não encontrado.");

        if (!cmd.IsPrivileged)
        {
            if (ticket.RequesterId != cmd.RequestingUserId)
                return Result.Failure("Chamado não encontrado.");

            if (ticket.Status is not (TicketStatus.New or TicketStatus.Triaged))
                return Result.Failure("O chamado não pode ser cancelado após ter sido iniciado.");
        }

        if (!ticket.RowVersion.SequenceEqual(cmd.RowVersion))
            return Result.Failure("O chamado foi alterado por outro usuário. Atualize a página e tente novamente.");

        var now = _time.GetUtcNow();
        try
        {
            ticket.Cancel(cmd.RequestingUserId, now, cmd.Reason);
            await _db.SaveChangesAsync(ct);
        }
        catch (DomainException ex) { return Result.Failure(ex.Message); }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure("O chamado foi alterado por outro usuário. Atualize a página e tente novamente.");
        }

        await _audit.LogAsync("TicketCancelled", "Ticket", cmd.TicketId.ToString(), cmd.RequestingUserId, ct: ct);
        return Result.Success();
    }
}
