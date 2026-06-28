namespace DeskFlow.Application.Features.Tickets.AddAttachment;

public sealed record AddAttachmentCommand(
    Guid TicketId,
    Guid UploadedByUserId,
    string OriginalFileName,
    string ContentType,
    long FileSize,
    Stream Content);
