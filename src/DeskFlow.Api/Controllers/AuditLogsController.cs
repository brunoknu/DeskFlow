using DeskFlow.Api.Policies;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize(Policy = AuthorizationPolicies.CanViewAuditLogs)]
public class AuditLogsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AuditLogsController(ApplicationDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? entityType,
        [FromQuery] string? entityId,
        [FromQuery] Guid? actorUserId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);

        var q = _db.AuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType)) q = q.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId)) q = q.Where(a => a.EntityId == entityId);
        if (actorUserId.HasValue) q = q.Where(a => a.ActorUserId == actorUserId.Value);

        var total = await q.CountAsync(ct);
        var logs = await q
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id, a.Action, a.EntityType, a.EntityId,
                a.ActorUserId, a.OccurredAtUtc, a.CorrelationId, a.Metadata
            })
            .ToListAsync(ct);

        return Ok(new { items = logs, total, page, pageSize });
    }
}
