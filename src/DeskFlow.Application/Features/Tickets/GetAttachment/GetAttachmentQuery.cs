namespace DeskFlow.Application.Features.Tickets.GetAttachment;

public sealed record GetAttachmentQuery(
    Guid AttachmentId,
    Guid TicketId,
    Guid RequestingUserId,
    bool IsPrivileged);

public sealed record AttachmentDownloadResult(
    Stream Content,
    string OriginalFileName,
    string ContentType);
