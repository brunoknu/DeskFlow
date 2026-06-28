using DeskFlow.Application.Common;
using DeskFlow.Domain.Entities;
using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Enums;
using DeskFlow.Infrastructure.Identity;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.AssignTicket;

public class AssignTicketHandler
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly TimeProvider _time;

    public AssignTicketHandler(
        ApplicationDbContext db,
        IAuditLogger audit,
        UserManager<ApplicationUser> userManager,
        TimeProvider time)
    {
        _db = db;
        _audit = audit;
        _userManager = userManager;
        _time = time;
    }

    public async Task<Result> HandleAsync(AssignTicketCommand cmd, CancellationToken ct)
    {
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == cmd.TicketId, ct);
        if (ticket is null)
            return Result.Failure("Ticket not found.");

        // Validate row version for optimistic concurrency
        if (!ticket.RowVersion.SequenceEqual(cmd.RowVersion))
            return Result.Failure("Ticket was modified by another user. Please refresh and try again.");

        if (cmd.AgentId.HasValue)
        {
            var agent = await _userManager.FindByIdAsync(cmd.AgentId.Value.ToString());
            if (agent is null || !agent.IsActive)
                return Result.Failure("Agent not found or is not active.");

            var isAgent = await _userManager.IsInRoleAsync(agent, UserRole.Agent)
                       || await _userManager.IsInRoleAsync(agent, UserRole.Manager);
            if (!isAgent)
                return Result.Failure("User is not an agent or manager.");
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
            return Result.Failure("Ticket was modified by another user. Please refresh and try again.");
        }

        await _audit.LogAsync("TicketAssigned", "Ticket", cmd.TicketId.ToString(), cmd.ChangedByUserId, ct: ct);
        return Result.Success();
    }
}
