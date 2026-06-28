using System.Security.Cryptography;
using DeskFlow.Application.Contracts;
using Microsoft.Extensions.Configuration;

namespace DeskFlow.Infrastructure.FileStorage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(IConfiguration config)
    {
        _basePath = config["Storage:AttachmentsPath"]
            ?? throw new InvalidOperationException("Storage:AttachmentsPath is not configured.");

        Directory.CreateDirectory(_basePath);
    }

    public async Task<(string StoredFileName, string StoragePath, string FileHash)> SaveAsync(
        Stream content, string extension, CancellationToken ct)
    {
        var storedFileName = $"{Guid.NewGuid()}{extension}";
        var storagePath = Path.Combine(_basePath, storedFileName);

        // Prevent path traversal: store only in base path
        var fullPath = Path.GetFullPath(storagePath);
        if (!fullPath.StartsWith(Path.GetFullPath(_basePath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid storage path detected.");

        string fileHash;
        using (var sha256 = SHA256.Create())
        using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            // Compute hash while writing to avoid second pass
            using var cryptoStream = new CryptoStream(fs, sha256, CryptoStreamMode.Write);
            await content.CopyToAsync(cryptoStream, ct);
            await cryptoStream.FlushFinalBlockAsync(ct);
            fileHash = Convert.ToHexString(sha256.Hash!).ToLowerInvariant();
        }

        return (storedFileName, storagePath, fileHash);
    }

    public async Task<Stream> ReadAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.GetFullPath(storagePath);
        if (!fullPath.StartsWith(Path.GetFullPath(_basePath), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Invalid storage path.");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Attachment file not found.");

        var memStream = new MemoryStream();
        using var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await fs.CopyToAsync(memStream, ct);
        memStream.Position = 0;
        return memStream;
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        var fullPath = Path.GetFullPath(storagePath);
        if (!fullPath.StartsWith(Path.GetFullPath(_basePath), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Invalid storage path.");

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
