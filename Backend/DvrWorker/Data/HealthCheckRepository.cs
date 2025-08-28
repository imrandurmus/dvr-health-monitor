using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using DvrWorker.Models;

namespace DvrWorker.Data;

public  class HealthCheckRepository : IHealthCheckRepository
{
    private readonly IMongoCollection<HealthCheckDoc> _collection;

    public HealthCheckRepository(IMongoDatabase db)
    {
        _collection = db.GetCollection<HealthCheckDoc>("healthchecks");
    }

    public async Task InsertAsync(HealthCheckDoc doc, CancellationToken ct)
    {
        await _collection.InsertOneAsync(doc, cancellationToken: ct);
    }
}
