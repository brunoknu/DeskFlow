using DeskFlow.Application.Common;
using DeskFlow.Domain.Exceptions;
using DeskFlow.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.AddInternalNote;

public class AddInternalNoteHandler
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _time;

    public AddInternalNoteHandler(IApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<Result<Guid>> HandleAsync(AddInternalNoteCommand cmd, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cmd.Content))
            return Result.Failure<Guid>("Note content is required.");

        var ticket = await _db.Tickets
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == cmd.TicketId, ct);

        if (ticket is null) return Result.Failure<Guid>("Ticket not found.");

        var now = _time.GetUtcNow();
        try
        {
            var note = ticket.AddInternalNote(cmd.AuthorId, cmd.Content, now);
            await _db.SaveChangesAsync(ct);
            return Result.Success(note.Id);
        }
        catch (DomainException ex) { return Result.Failure<Guid>(ex.Message); }
    }
}
