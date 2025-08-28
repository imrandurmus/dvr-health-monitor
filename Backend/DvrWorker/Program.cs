using DvrWorker;
using MongoDB.Driver;
using DvrWorker.Data;
using DvrWorker.Models;
using DvrWorker.Configurations;
using DvrWorker.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);


builder.Services.Configure<MongoOptions>(builder.Configuration.GetSection("Mongo"));


//Registering the MongoClient utilizing the options
builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<MongoOptions>>().Value;
    var mongoDb = new MongoClient(opts.ConnectionString);
    return mongoDb;
});

// Registering IMongoDatabase
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var opts = sp.GetRequiredService <IOptions<MongoOptions>>().Value;
    return sp.GetRequiredService<IMongoClient>().GetDatabase(opts.Database);
});

// binding the snapshot options we made in SnapshotOptions 
builder.Services
    .AddOptions<SnapshotOptions>()
    .Bind(builder.Configuration.GetSection("Snapshot"))
    .ValidateDataAnnotations()
    .Validate(o => o.IntervalSeconds >= 1, "Interval must be >= 1")
    .ValidateOnStart();

// Registering the snapshot service (typed Http for today's implementation)
builder.Services.AddHttpClient<ISnapshotService, HttpSnapshotService>();

builder.Services.AddSingleton<IDevicesRepository, DevicesRepository>();
builder.Services.AddHostedService<Worker>();

builder.Services.Configure<SnapshotStorageOptions>(
    builder.Configuration.GetSection("SnapshotStorage"));

builder.Services.AddSingleton<ISnapshotStorage, SnapshotStorage>();

builder.Services.AddSingleton<IImageAnalyzer, ImageAnalyzer>();
var host = builder.Build();
host.Run();
