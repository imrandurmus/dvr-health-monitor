using DvrWorker.Models;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DvrWorker.Data;

public sealed class SnapshotRepository
{
    private readonly IMongoCollection<SnapshotDoc> _col;

    public SnapshotRepository (IMongoDatabase db)
    {
        _col = db.GetCollection<SnapshotDoc>("Snapshots");
        var ix = Builders<SnapshotDoc>.IndexKeys
            .Ascending(x => x.DeviceId)
            .Descending(x => x.CreatedAtUtc);
        _col.Indexes.CreateOne ( new CreateIndexModel<SnapshotDoc> ( ix ) );
        _col.Indexes.CreateOne(new CreateIndexModel<SnapshotDoc>(
            Builders<SnapshotDoc>.IndexKeys.Ascending("Inspections.IsBlurry")));
        _col.Indexes.CreateOne(new CreateIndexModel<SnapshotDoc>(
            Builders<SnapshotDoc>.IndexKeys.Ascending("Inspections.IsSingleColor")));
    }

    public Task InserAsync(SnapshotDoc doc, CancellationToken ct)
        => _col.InsertOneAsync(doc, cancellationToken: ct);
}
