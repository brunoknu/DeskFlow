using DeskFlow.Domain.Enums;
using DeskFlow.Domain.Exceptions;

namespace DeskFlow.Domain.Entities;

public class Ticket
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 10_000;
    public const int ResolutionSummaryMaxLength = 5_000;
    public const int ReopenWindowDays = 7;

    private static readonly Dictionary<TicketStatus, HashSet<TicketStatus>> AllowedTransitions = new()
    {
        [TicketStatus.New]              = [TicketStatus.Triaged, TicketStatus.Cancelled],
        [TicketStatus.Triaged]          = [TicketStatus.InProgress, TicketStatus.Cancelled],
        [TicketStatus.InProgress]       = [TicketStatus.WaitingRequester, TicketStatus.WaitingThirdParty, TicketStatus.Resolved],
        [TicketStatus.WaitingRequester] = [TicketStatus.InProgress, TicketStatus.Resolved, TicketStatus.Cancelled],
        [TicketStatus.WaitingThirdParty]= [TicketStatus.InProgress, TicketStatus.Resolved],
        [TicketStatus.Resolved]         = [TicketStatus.Closed, TicketStatus.InProgress],
        [TicketStatus.Closed]           = [TicketStatus.InProgress], // reopen only
        [TicketStatus.Cancelled]        = []
    };

    public Guid Id { get; private set; }
    public string Number { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid RequesterId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid CategoryId { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketStatus Status { get; private set; }
    public Guid? AssignedAgentId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? FirstResponseAtUtc { get; private set; }
    public DateTimeOffset? ResolvedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public DateTimeOffset FirstResponseDueAtUtc { get; private set; }
    public DateTimeOffset ResolutionDueAtUtc { get; private set; }
    public string? ResolutionSummary { get; private set; }
    public int ReopenCount { get; private set; }

    // EF Core concurrency token
    public byte[] RowVersion { get; private set; } = [];

    private readonly List<TicketComment> _comments = [];
    private readonly List<TicketAttachment> _attachments = [];
    private readonly List<TicketStatusHistory> _statusHistory = [];
    private readonly List<TicketAssignmentHistory> _assignmentHistory = [];

    public IReadOnlyList<TicketComment> Comments => _comments.AsReadOnly();
    public IReadOnlyList<TicketAttachment> Attachments => _attachments.AsReadOnly();
    public IReadOnlyList<TicketStatusHistory> StatusHistory => _statusHistory.AsReadOnly();
    public IReadOnlyList<TicketAssignmentHistory> AssignmentHistory => _assignmentHistory.AsReadOnly();
    public TicketRating? Rating { get; private set; }

    private Ticket() { }

    public static Ticket Create(
        string number,
        string title,
        string description,
        Guid requesterId,
        Guid departmentId,
        Guid categoryId,
        TicketPriority priority,
        DateTimeOffset firstResponseDue,
        DateTimeOffset resolutionDue,
        DateTimeOffset now)
    {
        ValidateTitle(title);
        ValidateDescription(description);

        return new Ticket
        {
            Id = Guid.NewGuid(),
            Number = number,
            Title = title.Trim(),
            Description = description.Trim(),
            RequesterId = requesterId,
            DepartmentId = departmentId,
            CategoryId = categoryId,
            Priority = priority,
            Status = TicketStatus.New,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            FirstResponseDueAtUtc = firstResponseDue,
            ResolutionDueAtUtc = resolutionDue,
            ReopenCount = 0
        };
    }

    public void Transition(TicketStatus newStatus, Guid changedByUserId, DateTimeOffset now, string? reason = null)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidStatusTransitionException(Status, newStatus);

        var previous = Status;
        Status = newStatus;
        UpdatedAtUtc = now;

        if (newStatus == TicketStatus.Resolved)
            ResolvedAtUtc = now;
        else if (newStatus == TicketStatus.Closed)
            ClosedAtUtc = now;
        else if (newStatus == TicketStatus.InProgress && previous is TicketStatus.Resolved or TicketStatus.Closed)
        {
            // Reopen
            ReopenCount++;
            ResolvedAtUtc = null;
            ClosedAtUtc = null;
        }

        _statusHistory.Add(TicketStatusHistory.Create(Id, previous, newStatus, changedByUserId, now, reason));
    }

    public bool CanTransitionTo(TicketStatus newStatus) =>
        AllowedTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(newStatus);

    public void Assign(Guid? agentId, Guid changedByUserId, DateTimeOffset now, string? reason = null)
    {
        if (Status == TicketStatus.Cancelled)
            throw new DomainException("Cannot assign a cancelled ticket.");
        if (Status == TicketStatus.Closed)
            throw new DomainException("Cannot assign a closed ticket.");

        var previous = AssignedAgentId;
        AssignedAgentId = agentId;
        UpdatedAtUtc = now;

        _assignmentHistory.Add(TicketAssignmentHistory.Create(Id, previous, agentId, changedByUserId, now, reason));
    }

    public void Resolve(string resolutionSummary, Guid changedByUserId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(resolutionSummary))
            throw new DomainException("Resolution summary is required when resolving a ticket.");
        if (resolutionSummary.Length > ResolutionSummaryMaxLength)
            throw new DomainException($"Resolution summary cannot exceed {ResolutionSummaryMaxLength} characters.");

        ResolutionSummary = resolutionSummary.Trim();
        Transition(TicketStatus.Resolved, changedByUserId, now);
    }

    public void Reopen(Guid changedByUserId, DateTimeOffset now, DateTimeOffset? closedAt = null)
    {
        if (Status != TicketStatus.Resolved && Status != TicketStatus.Closed)
            throw new DomainException("Only resolved or closed tickets can be reopened.");

        var referenceDate = ClosedAtUtc ?? ResolvedAtUtc ?? now;
        var windowExpired = now > referenceDate.AddDays(ReopenWindowDays);
        if (windowExpired)
            throw new DomainException($"Reopen window of {ReopenWindowDays} days has expired.");

        Transition(TicketStatus.InProgress, changedByUserId, now, "Ticket reopened.");
        ResolutionSummary = null;
    }

    public void Cancel(Guid changedByUserId, DateTimeOffset now, string? reason = null)
    {
        if (Status == TicketStatus.Cancelled)
            throw new DomainException("Ticket is already cancelled.");
        if (Status is TicketStatus.Resolved or TicketStatus.Closed)
            throw new DomainException("Cannot cancel a resolved or closed ticket.");

        Transition(TicketStatus.Cancelled, changedByUserId, now, reason);
    }

    public void RecordFirstResponse(DateTimeOffset now)
    {
        if (FirstResponseAtUtc.HasValue) return;
        FirstResponseAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void UpdatePriority(TicketPriority newPriority, DateTimeOffset now)
    {
        Priority = newPriority;
        UpdatedAtUtc = now;
    }

    public TicketComment AddPublicComment(Guid authorId, string content, DateTimeOffset now)
    {
        if (Status is TicketStatus.Closed or TicketStatus.Cancelled)
            throw new DomainException("Cannot add comments to a closed or cancelled ticket.");

        var comment = TicketComment.CreatePublic(Id, authorId, content, now);
        _comments.Add(comment);
        UpdatedAtUtc = now;
        return comment;
    }

    public TicketComment AddInternalNote(Guid authorId, string content, DateTimeOffset now)
    {
        if (Status is TicketStatus.Closed or TicketStatus.Cancelled)
            throw new DomainException("Cannot add notes to a closed or cancelled ticket.");

        var note = TicketComment.CreateInternal(Id, authorId, content, now);
        _comments.Add(note);
        UpdatedAtUtc = now;
        return note;
    }

    public TicketAttachment AddAttachment(
        Guid uploadedByUserId,
        string originalFileName,
        string storedFileName,
        string contentType,
        long fileSize,
        string storagePath,
        string fileHash,
        DateTimeOffset now)
    {
        if (_attachments.Count >= TicketAttachment.MaxAttachmentsPerTicket)
            throw new DomainException($"Maximum of {TicketAttachment.MaxAttachmentsPerTicket} attachments per ticket.");

        var attachment = TicketAttachment.Create(
            Id, uploadedByUserId, originalFileName, storedFileName,
            contentType, fileSize, storagePath, fileHash, now);
        _attachments.Add(attachment);
        UpdatedAtUtc = now;
        return attachment;
    }

    public TicketRating AddRating(Guid requesterId, int score, string? comment, DateTimeOffset now)
    {
        if (requesterId != RequesterId)
            throw new DomainException("Only the ticket requester can rate the ticket.");
        if (Status is not (TicketStatus.Resolved or TicketStatus.Closed))
            throw new DomainException("Ticket can only be rated after resolution or closure.");
        if (Rating != null)
            throw new DomainException("Ticket has already been rated.");

        Rating = TicketRating.Create(Id, requesterId, score, comment, now);
        UpdatedAtUtc = now;
        return Rating;
    }

    public bool IsFirstResponseBreached(DateTimeOffset now) =>
        !FirstResponseAtUtc.HasValue && now > FirstResponseDueAtUtc;

    public bool IsResolutionBreached(DateTimeOffset now) =>
        Status is not (TicketStatus.Resolved or TicketStatus.Closed or TicketStatus.Cancelled)
        && now > ResolutionDueAtUtc;

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (title.Length > TitleMaxLength)
            throw new ArgumentException($"Title cannot exceed {TitleMaxLength} characters.", nameof(title));
    }

    private static void ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required.", nameof(description));
        if (description.Length > DescriptionMaxLength)
            throw new ArgumentException($"Description cannot exceed {DescriptionMaxLength} characters.", nameof(description));
    }
}
