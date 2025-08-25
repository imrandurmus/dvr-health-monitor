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
    return new MongoClient(opts.ConnectionString);
});

// Registering IMongoDatabase
builder.Services.AddSingleton<IMongoDatabase>(sp =>
{
    var opts = sp.GetRequiredService <IOptions<MongoOptions>>().Value;
    return sp.GetRequiredService<IMongoClient>().GetDatabase(opts.Database);
});

// binding the snapshot options we made in SnapshotOptions 
builder.Services.Configure<SnapshotOptions>(builder.Configuration.GetSection("Snapshot"));

// Registering the snapshot service (typed Http for today's implementation)
builder.Services.AddHttpClient<ISnapshotService, HttpSnapshotService>();

builder.Services.AddSingleton<IDevicesRepository, DevicesRepository>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
