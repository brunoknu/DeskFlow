using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Entities;
using DeskFlow.Infrastructure.Persistence;

namespace DeskFlow.Infrastructure.Audit;

public class AuditLogService : IAuditLogger
{
    private readonly ApplicationDbContext _db;
    private readonly TimeProvider _time;

    public AuditLogService(ApplicationDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task LogAsync(
        string action,
        string entityType,
        string entityId,
        Guid? actorUserId,
        string? correlationId = null,
        string? ipAddress = null,
        string? metadata = null,
        CancellationToken ct = default)
    {
        var log = AuditLog.Create(
            action, entityType, entityId, actorUserId,
            _time.GetUtcNow(), correlationId, ipAddress, metadata);

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }
}
