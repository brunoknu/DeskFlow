using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.GetTicketById;

public class GetTicketByIdHandler
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _time;

    public GetTicketByIdHandler(IApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<Result<TicketDetailResponse>> HandleAsync(GetTicketByIdQuery query, CancellationToken ct)
    {
        var ticket = await _db.Tickets
            .Include(t => t.Comments)
            .Include(t => t.Attachments)
            .Include(t => t.Rating)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.TicketId, ct);

        if (ticket is null)
            return Result.Failure<TicketDetailResponse>("Ticket not found.");

        // Requesters can only see their own tickets
        if (!query.IsPrivileged && ticket.RequesterId != query.RequestingUserId)
            return Result.Failure<TicketDetailResponse>("Ticket not found.");

        var userIds = new HashSet<Guid> { ticket.RequesterId };
        if (ticket.AssignedAgentId.HasValue) userIds.Add(ticket.AssignedAgentId.Value);
        foreach (var c in ticket.Comments) userIds.Add(c.AuthorId);

        var users = await _db.AppUsers
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var department = await _db.Departments
            .Where(d => d.Id == ticket.DepartmentId)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(ct) ?? "Unknown";

        var category = await _db.TicketCategories
            .Where(c => c.Id == ticket.CategoryId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct) ?? "Unknown";

        var now = _time.GetUtcNow();

        // Internal notes are filtered at query level to prevent accidental disclosure.
        var visibleComments = ticket.Comments
            .Where(c => !c.IsInternal || query.IsPrivileged)
            .Select(c => new CommentResponse(
                c.Id,
                c.AuthorId,
                users.GetValueOrDefault(c.AuthorId, "Unknown"),
                c.Content,
                c.IsInternal,
                c.CreatedAtUtc))
            .ToList();

        var attachments = ticket.Attachments
            .Select(a => new AttachmentResponse(
                a.Id,
                a.OriginalFileName,
                a.ContentType,
                a.FileSize,
                a.CreatedAtUtc))
            .ToList();

        var rating = ticket.Rating is null ? null
            : new RatingResponse(ticket.Rating.Score, ticket.Rating.Comment, ticket.Rating.CreatedAtUtc);

        return Result.Success(new TicketDetailResponse(
            ticket.Id,
            ticket.Number,
            ticket.Title,
            ticket.Description,
            ticket.RequesterId,
            users.GetValueOrDefault(ticket.RequesterId, "Unknown"),
            ticket.DepartmentId,
            department,
            ticket.CategoryId,
            category,
            ticket.Priority,
            ticket.Status,
            ticket.AssignedAgentId,
            ticket.AssignedAgentId.HasValue ? users.GetValueOrDefault(ticket.AssignedAgentId.Value) : null,
            ticket.CreatedAtUtc,
            ticket.UpdatedAtUtc,
            ticket.FirstResponseAtUtc,
            ticket.ResolvedAtUtc,
            ticket.ClosedAtUtc,
            ticket.FirstResponseDueAtUtc,
            ticket.ResolutionDueAtUtc,
            ticket.ResolutionSummary,
            ticket.ReopenCount,
            ticket.IsFirstResponseBreached(now),
            ticket.IsResolutionBreached(now),
            ticket.RowVersion,
            visibleComments,
            attachments,
            rating));
    }
}
