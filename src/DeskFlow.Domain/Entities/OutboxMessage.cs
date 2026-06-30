namespace DeskFlow.Domain.Entities;

public class OutboxMessage
{
    public const int MaxAttempts = 5;

    private static readonly int[] BackoffMinutes = [1, 5, 15, 30, 60];

    public Guid Id { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? NextAttemptAtUtc { get; private set; }
    public string? LastError { get; private set; }

    private OutboxMessage() { }

    public static OutboxMessage Create(string type, string payload, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = type,
            Payload = payload,
            OccurredAtUtc = now,
            AttemptCount = 0,
            NextAttemptAtUtc = now
        };
    }

    public void MarkProcessed(DateTimeOffset now)
    {
        ProcessedAtUtc = now;
        LastError = null;
    }

    public void RecordFailure(string error, DateTimeOffset now)
    {
        AttemptCount++;
        // Trunca o erro para evitar armazenar stack traces sensíveis.
        LastError = error.Length > 500 ? error[..500] : error;

        if (AttemptCount < MaxAttempts)
        {
            var backoff = BackoffMinutes[Math.Min(AttemptCount - 1, BackoffMinutes.Length - 1)];
            NextAttemptAtUtc = now.AddMinutes(backoff);
        }
        else
        {
            NextAttemptAtUtc = null; // exhausted
        }
    }

    public bool IsExhausted => AttemptCount >= MaxAttempts && ProcessedAtUtc == null;
    public bool IsProcessed => ProcessedAtUtc.HasValue;
}
