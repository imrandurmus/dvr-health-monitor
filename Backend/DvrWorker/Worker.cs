using DvrWorker.Data;
using DvrWorker.Configurations;
using DvrWorker.Services;
using Microsoft.Extensions.Options;

namespace DvrWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDevicesRepository _repo;
    private readonly ISnapshotService _snapshot;
    private readonly SnapshotOptions _snapOptions;

    public Worker(
        ILogger<Worker> logger,
        IDevicesRepository repo,
        ISnapshotService snapshot,
        IOptions<SnapshotOptions> snapOptions)
    {
        _logger = logger;
        _repo = repo;
        _snapshot = snapshot;
        _snapOptions = snapOptions.Value;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _repo.SeedIfEmptyAsync(stoppingToken);

        Directory.CreateDirectory(_snapOptions.OutputDir);
        int counter = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
             // device listing
            var devices = await _repo.GetEnabledAsync(stoppingToken);

            if (devices.Count == 0)
            {
                _logger.LogInformation("No enabled devices found.");
            }
            else
            {
                _logger.LogInformation("Found {n} devices", devices.Count);
                foreach (var d in devices)
                {
                    _logger.LogInformation("Camera {Name} {Ip} - {Count} channels",
                        d.Name, d.Ip, d.Channels.Count);
                }
            }

            //fetch and save snapshot (currently fake)
            try
            {
                var bytes = await _snapshot.GetSnapshotAsync(_snapOptions.Url, stoppingToken);
                var path = Path.Combine(_snapOptions.OutputDir, $"snap_{counter++}.jpg");
                await File.WriteAllBytesAsync(path, bytes, stoppingToken);
                _logger.LogInformation("Saved snapshot: {Path} ({size} bytes)", path, bytes.Length);
                _logger.LogInformation("Snapshot output directory: {dir}", _snapOptions.OutputDir);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Snapshot fetch failed from {Url}", _snapOptions.Url);
            }
            
            var interval = Math.Max(1, _snapOptions.IntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }
}
