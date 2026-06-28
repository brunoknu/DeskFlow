using DeskFlow.Application.Common;
using DeskFlow.Domain.Enums;
using DeskFlow.Infrastructure.Identity;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.SearchTickets;

public class SearchTicketsHandler
{
    private static readonly HashSet<string> AllowedSortFields =
        ["CreatedAtUtc", "UpdatedAtUtc", "Priority", "Status", "ResolutionDueAtUtc", "Number"];

    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;

    public SearchTicketsHandler(ApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<Result<PagedResult<TicketSummaryResponse>>> HandleAsync(SearchTicketsQuery query, CancellationToken ct)
    {
        if (query.Page < 1) return Result.Failure<PagedResult<TicketSummaryResponse>>("Page must be >= 1.");
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _db.Tickets.AsNoTracking().AsQueryable();

        // Requesters only see their own tickets
        if (!query.IsPrivileged)
            q = q.Where(t => t.RequesterId == query.RequestingUserId);

        if (query.Status.HasValue) q = q.Where(t => t.Status == query.Status.Value);
        if (query.Priority.HasValue) q = q.Where(t => t.Priority == query.Priority.Value);
        if (query.AssignedAgentId.HasValue) q = q.Where(t => t.AssignedAgentId == query.AssignedAgentId.Value);
        if (query.CategoryId.HasValue) q = q.Where(t => t.CategoryId == query.CategoryId.Value);
        if (query.DepartmentId.HasValue) q = q.Where(t => t.DepartmentId == query.DepartmentId.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(t => t.Title.Contains(term) || t.Number.Contains(term));
        }

        var now = _time.GetUtcNow();
        if (query.IsOverdue == true)
            q = q.Where(t =>
                t.Status != TicketStatus.Resolved &&
                t.Status != TicketStatus.Closed &&
                t.Status != TicketStatus.Cancelled &&
                t.ResolutionDueAtUtc < now);

        var total = await q.CountAsync(ct);

        // Whitelist sort to prevent injection
        var sortField = AllowedSortFields.Contains(query.SortBy) ? query.SortBy : "CreatedAtUtc";
        q = (sortField, query.SortDescending) switch
        {
            ("CreatedAtUtc", true)      => q.OrderByDescending(t => t.CreatedAtUtc),
            ("CreatedAtUtc", false)     => q.OrderBy(t => t.CreatedAtUtc),
            ("UpdatedAtUtc", true)      => q.OrderByDescending(t => t.UpdatedAtUtc),
            ("UpdatedAtUtc", false)     => q.OrderBy(t => t.UpdatedAtUtc),
            ("Priority", true)          => q.OrderByDescending(t => t.Priority),
            ("Priority", false)         => q.OrderBy(t => t.Priority),
            ("Status", true)            => q.OrderByDescending(t => t.Status),
            ("Status", false)           => q.OrderBy(t => t.Status),
            ("ResolutionDueAtUtc", true)=> q.OrderByDescending(t => t.ResolutionDueAtUtc),
            ("ResolutionDueAtUtc", false)=>q.OrderBy(t => t.ResolutionDueAtUtc),
            ("Number", true)            => q.OrderByDescending(t => t.Number),
            _                           => q.OrderBy(t => t.Number)
        };

        var tickets = await q
            .Skip((query.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var userIds = tickets.SelectMany(t => new[] { (Guid?)t.RequesterId, t.AssignedAgentId })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();

        var users = await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var catIds = tickets.Select(t => t.CategoryId).Distinct().ToList();
        var deptIds = tickets.Select(t => t.DepartmentId).Distinct().ToList();

        var cats = await _db.TicketCategories
            .Where(c => catIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var depts = await _db.Departments
            .Where(d => deptIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Name })
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var items = tickets.Select(t => new TicketSummaryResponse(
            t.Id, t.Number, t.Title, t.Priority, t.Status,
            users.GetValueOrDefault(t.RequesterId, "Unknown"),
            t.AssignedAgentId.HasValue ? users.GetValueOrDefault(t.AssignedAgentId.Value) : null,
            cats.GetValueOrDefault(t.CategoryId, "Unknown"),
            depts.GetValueOrDefault(t.DepartmentId, "Unknown"),
            t.CreatedAtUtc,
            t.ResolutionDueAtUtc,
            t.IsFirstResponseBreached(now),
            t.IsResolutionBreached(now)
        )).ToList();

        return Result.Success(new PagedResult<TicketSummaryResponse>(items, total, query.Page, pageSize));
    }
}
