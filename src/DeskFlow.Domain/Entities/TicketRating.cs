using DeskFlow.Domain.Exceptions;

namespace DeskFlow.Domain.Entities;

public class TicketRating
{
    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid RequesterId { get; private set; }
    public int Score { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private TicketRating() { }

    public static TicketRating Create(Guid ticketId, Guid requesterId, int score, string? comment, DateTimeOffset now)
    {
        if (score < 1 || score > 5)
            throw new DomainException("Rating score must be between 1 and 5.");

        return new TicketRating
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            RequesterId = requesterId,
            Score = score,
            Comment = comment?.Trim(),
            CreatedAtUtc = now
        };
    }
}
