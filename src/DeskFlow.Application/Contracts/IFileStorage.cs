namespace DeskFlow.Application.Contracts;

public interface IFileStorage
{
    Task<(string StoredFileName, string StoragePath, string FileHash)> SaveAsync(
        Stream content, string extension, CancellationToken ct);

    Task<Stream> ReadAsync(string storagePath, CancellationToken ct);

    Task DeleteAsync(string storagePath, CancellationToken ct);
}
