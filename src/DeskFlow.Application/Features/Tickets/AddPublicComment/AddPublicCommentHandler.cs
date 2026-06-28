using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Exceptions;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.AddPublicComment;

public class AddPublicCommentHandler
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditLogger _audit;
    private readonly TimeProvider _time;

    public AddPublicCommentHandler(ApplicationDbContext db, IAuditLogger audit, TimeProvider time)
    {
        _db = db;
        _audit = audit;
        _time = time;
    }

    public async Task<Result<Guid>> HandleAsync(AddPublicCommentCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content))
            return Result.Failure<Guid>("Comment content is required.");

        var ticket = await _db.Tickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == cmd.TicketId, ct);

        if (ticket is null)
            return Result.Failure<Guid>("Ticket not found.");

        var now = _time.GetUtcNow();
        try
        {
            var comment = ticket.AddPublicComment(cmd.AuthorId, cmd.Content, now);

            // First public agent comment records the SLA first response
            if (cmd.AuthorIsAgent)
                ticket.RecordFirstResponse(now);

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                TicketId = ticket.Id,
                Number = ticket.Number,
                AuthorId = cmd.AuthorId
            });
            _db.OutboxMessages.Add(OutboxMessage.Create("TicketCommentAdded", payload, now));

            await _db.SaveChangesAsync(ct);
            return Result.Success(comment.Id);
        }
        catch (DomainException ex) { return Result.Failure<Guid>(ex.Message); }
    }
}
