using DeskFlow.Application.Common;
using DeskFlow.Application.Contracts;
using DeskFlow.Domain.Entities;
using DeskFlow.Domain.Exceptions;
using DeskFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DeskFlow.Application.Features.Tickets.AddAttachment;

public class AddAttachmentHandler
{
    private readonly ApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly TimeProvider _time;

    public AddAttachmentHandler(ApplicationDbContext db, IFileStorage storage, TimeProvider time)
    {
        _db = db;
        _storage = storage;
        _time = time;
    }

    public async Task<Result<Guid>> HandleAsync(AddAttachmentCommand cmd, CancellationToken ct)
    {
        if (!TicketAttachment.IsExtensionAllowed(cmd.OriginalFileName))
            return Result.Failure<Guid>("File type not allowed.");

        if (cmd.FileSize > TicketAttachment.MaxFileSizeBytes)
            return Result.Failure<Guid>($"File exceeds maximum size of {TicketAttachment.MaxFileSizeBytes / 1024 / 1024} MB.");

        var ticket = await _db.Tickets
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == cmd.TicketId, ct);

        if (ticket is null) return Result.Failure<Guid>("Ticket not found.");

        var ext = Path.GetExtension(cmd.OriginalFileName).ToLowerInvariant();
        var now = _time.GetUtcNow();

        var (storedFileName, storagePath, fileHash) = await _storage.SaveAsync(cmd.Content, ext, ct);

        try
        {
            var attachment = ticket.AddAttachment(
                cmd.UploadedByUserId, cmd.OriginalFileName, storedFileName,
                cmd.ContentType, cmd.FileSize, storagePath, fileHash, now);

            await _db.SaveChangesAsync(ct);
            return Result.Success(attachment.Id);
        }
        catch (DomainException ex)
        {
            // Clean up saved file if domain rule rejected the attachment
            await _storage.DeleteAsync(storagePath, ct);
            return Result.Failure<Guid>(ex.Message);
        }
    }
}
