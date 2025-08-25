namespace DvrWorker.Services;


public interface ISnapshotService
{
    Task<byte[]> GetSnapshotAsync(string url, CancellationToken ct);
}