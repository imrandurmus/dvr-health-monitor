using MongoDB.Driver;
using DvrWorker.Models;

namespace DvrWorker.Data;

public interface IDevicesRepository
{
    Task<List<Device>> GetEnabledAsync(CancellationToken ct);
    Task SeedIfEmptyAsync(CancellationToken ct);
}

public sealed class DevicesRepository : IDevicesRepository
{
    private readonly IMongoCollection<Device> _col;

    public DevicesRepository(IMongoDatabase db)
    {
        _col = db.GetCollection<Device>("Devices");
    }

    public async Task<List<Device>> GetEnabledAsync(CancellationToken ct) =>
        await _col.Find(d => d.IsEnabled).ToListAsync(ct);

    public async Task SeedIfEmptyAsync(CancellationToken ct)
    {
        var count = await _col.CountDocumentsAsync(_ => true, cancellationToken: ct);
        if (count > 0) return;
        var seed = new[]
                    {
            new Device
            {
                Name = "Lab-DVR-01",
                Site = "HQ",
                Ip = "192.168.1.50",
                Channels = new() { new() { Id = 101, Label = "Back" }, new() { Id = 102, Label = "Lobby" } }
            },

            new Device
            {
                Name = "Lab-DVR-02",
                Site = "HQ",
                Ip = "192.168.1.51",
                Channels = new() { new() { Id = 101, Label = "Back" }, new() { Id = 104, Label = "Parking" } }
            }

        };


        await _col.InsertManyAsync(seed, cancellationToken: ct);


        await _col.Indexes.CreateOneAsync(
            new CreateIndexModel<Device>(
                Builders<Device>.IndexKeys.Ascending(d => d.IsEnabled).Ascending(d => d.Site)),
            cancellationToken: ct);
    }
}