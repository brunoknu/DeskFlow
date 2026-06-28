namespace DeskFlow.Application.Contracts;

public interface IAuditLogger
{
    Task LogAsync(
        string action,
        string entityType,
        string entityId,
        Guid? actorUserId,
        string? correlationId = null,
        string? ipAddress = null,
        string? metadata = null,
        CancellationToken ct = default);
}
