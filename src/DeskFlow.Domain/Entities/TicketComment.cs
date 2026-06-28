namespace DeskFlow.Domain.Entities;

public class TicketComment
{
    public const int MaxContentLength = 10_000;

    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsInternal { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }

    private TicketComment() { }

    public static TicketComment CreatePublic(Guid ticketId, Guid authorId, string content, DateTimeOffset now)
    {
        ValidateContent(content);
        return new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorId = authorId,
            Content = content.Trim(),
            IsInternal = false,
            CreatedAtUtc = now
        };
    }

    public static TicketComment CreateInternal(Guid ticketId, Guid authorId, string content, DateTimeOffset now)
    {
        ValidateContent(content);
        return new TicketComment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            AuthorId = authorId,
            Content = content.Trim(),
            IsInternal = true,
            CreatedAtUtc = now
        };
    }

    private static void ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));
        if (content.Length > MaxContentLength)
            throw new ArgumentException($"Comment content cannot exceed {MaxContentLength} characters.", nameof(content));
    }
}
