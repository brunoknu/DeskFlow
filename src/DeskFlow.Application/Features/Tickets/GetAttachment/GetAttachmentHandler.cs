using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.GetAttachment;

public class GetAttachmentHandler
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorage _storage;

    public GetAttachmentHandler(ApplicationDbContext db, IFileStorage storage)
    {
        _db = db;
        _storage = storage;
    }

    public async Task<Result<AttachmentDownloadResult>> HandleAsync(GetAttachmentQuery query, CancellationToken ct)
    {
        var attachment = await _db.TicketAttachments
            .FirstOrDefaultAsync(a => a.Id == query.AttachmentId && a.TicketId == query.TicketId, ct);

        if (attachment is null)
            return Result.Failure<AttachmentDownloadResult>("Attachment not found.");

        // Verify requester owns the ticket or is privileged
        if (!query.IsPrivileged)
        {
            var ticket = await _db.Tickets
                .Where(t => t.Id == query.TicketId)
                .Select(t => new { t.RequesterId })
                .FirstOrDefaultAsync(ct);

            if (ticket is null || ticket.RequesterId != query.RequestingUserId)
                return Result.Failure<AttachmentDownloadResult>("Attachment not found.");
        }

        var stream = await _storage.ReadAsync(attachment.StoragePath, ct);
        return Result.Success(new AttachmentDownloadResult(stream, attachment.OriginalFileName, attachment.ContentType));
    }
}
