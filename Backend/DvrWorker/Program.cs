using DvrWorker;
using MongoDB.Driver;
using DvrWorker.Data;
using DvrWorker.Models;
using DvrWorker.Configurations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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


builder.Services.AddSingleton<IDevicesRepository, DevicesRepository>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
