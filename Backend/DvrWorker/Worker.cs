using DvrWorker.Data;
using DvrWorker.Configurations;
using DvrWorker.Services;
using Microsoft.Extensions.Options;
using System.Linq;

namespace DvrWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDevicesRepository _repo;
    private readonly ISnapshotService _snapshot;
    private readonly SnapshotOptions _snapOptions;
    private readonly ISnapshotStorage _snapshotStorage;

    private DateOnly _lastCleanup = DateOnly.MinValue;

    public Worker(
        ILogger<Worker> logger,
        IDevicesRepository repo,
        ISnapshotService snapshot,
        IOptions<SnapshotOptions> snapOptions,
        ISnapshotStorage snapsStore)
    {
        _logger = logger;
        _repo = repo;
        _snapshot = snapshot;
        _snapOptions = snapOptions.Value;
        _snapshotStorage = snapsStore;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _repo.SeedIfEmptyAsync(stoppingToken);

        
        

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
                _logger.LogInformation("Found {Count} devices", devices.Count);
                foreach (var d in devices)
                {
                    _logger.LogInformation("Camera {Name} {Ip} - {ChannelCount} channels",
                        d.Name, d.Ip, d.Channels.Count);
                }
            }

            //fetch and save snapshot (currently fake)
            foreach (var d in devices)
            {
                foreach (var ch in d.Channels.Where(c => c.Enabled))
                {
                    try
                    {
                        //Stub fetch
                        var jpegBytes = await _snapshot.GetSnapshotAsync(_snapOptions.Url, stoppingToken);
                        if (jpegBytes is null || jpegBytes.Length == 0)
                        {
                            _logger.LogWarning("Empty snapshot for {Device}/{Channel}", d.Name ?? d.Id, ch.Id);
                            continue;
                        }
                        var savedPath = await _snapshotStorage.SaveAsync(
                        device: d,
                        channelId: ch.Id,
                        jpegBytes: jpegBytes,
                        when: DateTimeOffset.UtcNow,
                        ct: stoppingToken);
                        _logger.LogInformation("Saved snapshot d={Device} ch={Channel} -> {Path}",
                            d.Name ?? d.Id, ch.Id, savedPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Snapshot fetch/save failed for {Device}/{Channel}", d.Name ?? d.Id, ch.Id);
                    }
                }
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if(today != _lastCleanup)
            {
                _lastCleanup = today;
                try
                {
                    var deleted = await _snapshotStorage.CleanupOldAsync(stoppingToken);
                    if (deleted > 0)
                        _logger.LogInformation("Snapshot cleanup removed {Count} old files.", deleted);
                }
                catch(Exception ex)
                {
                    _logger.LogWarning(ex, "Snapshot cleanup failed");
                }
            }
            var interval = Math.Max(1, _snapOptions.IntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(interval), stoppingToken);
        }
    }
}
