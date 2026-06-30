using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.AssignTicket;

public class AssignTicketHandler
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly IUserService _users;
    private readonly TimeProvider _time;

    public AssignTicketHandler(
        IApplicationDbContext db,
        IAuditLogger audit,
        IUserService users,
        TimeProvider time)
    {
        _db = db;
        _audit = audit;
        _users = users;
        _time = time;
    }

    public async Task<Result> HandleAsync(AssignTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == cmd.TicketId, ct);
        if (ticket is null)
            return Result.Failure("Chamado não encontrado.");

        if (!ticket.RowVersion.SequenceEqual(cmd.RowVersion))
            return Result.Failure("O chamado foi alterado por outro usuário. Atualize a página e tente novamente.");

        if (cmd.AgentId.HasValue)
        {
            if (!await _users.IsActiveAsync(cmd.AgentId.Value, ct))
                return Result.Failure("Agente não encontrado ou inativo.");

            if (!await _users.IsAgentOrManagerAsync(cmd.AgentId.Value, ct))
                return Result.Failure("O usuário não possui perfil de agente ou gestor.");
        }

        var now = _time.GetUtcNow();
        try
        {
            ticket.Assign(cmd.AgentId, cmd.ChangedByUserId, now, cmd.Reason);

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                TicketId = ticket.Id,
                Number = ticket.Number,
                AgentId = cmd.AgentId
            });
            _db.OutboxMessages.Add(OutboxMessage.Create("TicketAssigned", payload, now));

            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure("O chamado foi alterado por outro usuário. Atualize a página e tente novamente.");
        }

        await _audit.LogAsync("TicketAssigned", "Ticket", cmd.TicketId.ToString(), cmd.ChangedByUserId, ct: ct);
        return Result.Success();
    }
}
