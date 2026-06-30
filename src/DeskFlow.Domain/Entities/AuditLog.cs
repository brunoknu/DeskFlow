namespace DeskFlow.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string? CorrelationId { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Metadata { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string action,
        string entityType,
        string entityId,
        Guid? actorUserId,
        DateTimeOffset occurredAtUtc,
        string? correlationId = null,
        string? ipAddress = null,
        string? metadata = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityId);

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            ActorUserId = actorUserId,
            OccurredAtUtc = occurredAtUtc,
            CorrelationId = correlationId,
            // Armazena apenas os 3 primeiros octetos para anonimização parcial (ex.: "192.168.1.***").
            IpAddress = AnonymizeIp(ipAddress),
            Metadata = metadata
        };
    }

    private static string? AnonymizeIp(string? ip)
    {
        if (string.IsNullOrEmpty(ip)) return null;
        var parts = ip.Split('.');
        if (parts.Length == 4)
            return $"{parts[0]}.{parts[1]}.{parts[2]}.*";
        // IPv6: mantém os primeiros 4 grupos.
        var v6Parts = ip.Split(':');
        if (v6Parts.Length >= 4)
            return string.Join(":", v6Parts[..4]) + ":***";
        return "***";
    }
}
