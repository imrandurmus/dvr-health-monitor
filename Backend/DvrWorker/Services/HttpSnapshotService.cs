using System.Net.Http;

namespace DvrWorker.Services;

public sealed class HttpSnapshotService : ISnapshotService
{
    private readonly HttpClient _http;

    public HttpSnapshotService(HttpClient http)
    {
        _http = http;
    }

    public Task<byte[]> GetSnapshotAsync(string url, CancellationToken ct) =>
        _http.GetByteArrayAsync(url, ct);
}