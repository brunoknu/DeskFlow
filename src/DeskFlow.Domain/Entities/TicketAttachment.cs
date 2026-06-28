namespace DeskFlow.Domain.Entities;

public class TicketAttachment
{
    public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    public const int MaxAttachmentsPerTicket = 5;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".png", ".jpg", ".jpeg", ".txt", ".docx", ".xlsx" };

    private static readonly HashSet<string> BlockedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".exe", ".bat", ".cmd", ".ps1", ".js", ".html", ".svg", ".dll", ".zip", ".rar" };

    public Guid Id { get; private set; }
    public Guid TicketId { get; private set; }
    public Guid UploadedByUserId { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoredFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public string FileHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    private TicketAttachment() { }

    public static TicketAttachment Create(
        Guid ticketId,
        Guid uploadedByUserId,
        string originalFileName,
        string storedFileName,
        string contentType,
        long fileSize,
        string storagePath,
        string fileHash,
        DateTimeOffset now)
    {
        ValidateFileName(originalFileName);
        if (fileSize <= 0 || fileSize > MaxFileSizeBytes)
            throw new ArgumentException($"File size must be between 1 byte and {MaxFileSizeBytes} bytes.", nameof(fileSize));
        if (string.IsNullOrWhiteSpace(fileHash))
            throw new ArgumentException("File hash is required.", nameof(fileHash));

        return new TicketAttachment
        {
            Id = Guid.NewGuid(),
            TicketId = ticketId,
            UploadedByUserId = uploadedByUserId,
            OriginalFileName = SanitizeFileName(originalFileName),
            StoredFileName = storedFileName,
            ContentType = contentType,
            FileSize = fileSize,
            StoragePath = storagePath,
            FileHash = fileHash,
            CreatedAtUtc = now
        };
    }

    public static bool IsExtensionAllowed(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return AllowedExtensions.Contains(ext) && !BlockedExtensions.Contains(ext);
    }

    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name cannot be empty.", nameof(fileName));

        var ext = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException($"File extension '{ext}' is not allowed.", nameof(fileName));
    }

    private static string SanitizeFileName(string fileName)
    {
        // Keep only the file name, strip path components (path traversal protection)
        var name = Path.GetFileName(fileName);
        // Replace any remaining unsafe characters
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
