using DeskFlow.Domain.Enums;

namespace DeskFlow.Application.Features.Tickets.GetTicketById;

public sealed record TicketDetailResponse(
    Guid Id,
    string Number,
    string Title,
    string Description,
    Guid RequesterId,
    string RequesterName,
    Guid DepartmentId,
    string DepartmentName,
    Guid CategoryId,
    string CategoryName,
    TicketPriority Priority,
    TicketStatus Status,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? FirstResponseAtUtc,
    DateTimeOffset? ResolvedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    DateTimeOffset FirstResponseDueAtUtc,
    DateTimeOffset ResolutionDueAtUtc,
    string? ResolutionSummary,
    int ReopenCount,
    bool IsFirstResponseBreached,
    bool IsResolutionBreached,
    byte[] RowVersion,
    IReadOnlyList<CommentResponse> Comments,
    IReadOnlyList<AttachmentResponse> Attachments,
    RatingResponse? Rating);

public sealed record CommentResponse(
    Guid Id,
    Guid AuthorId,
    string AuthorName,
    string Content,
    bool IsInternal,
    DateTimeOffset CreatedAtUtc);

public sealed record AttachmentResponse(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    DateTimeOffset CreatedAtUtc);

public sealed record RatingResponse(
    int Score,
    string? Comment,
    DateTimeOffset CreatedAtUtc);
