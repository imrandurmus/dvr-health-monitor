using DvrWorker.Data;
using DvrWorker.Configurations;
using DvrWorker.Services;
using Microsoft.Extensions.Options;
using System.Linq;
using DvrWorker.Models;

namespace DvrWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDevicesRepository _repo;
    private readonly ISnapshotService _snapshot;
    private readonly SnapshotOptions _snapOptions;
    private readonly ISnapshotStorage _snapshotStorage;
    private readonly IImageAnalyzer _analyzer;
    private readonly IHealthCheckRepository _healthCheckRepo;

    private DateOnly _lastCleanup = DateOnly.MinValue;

    public Worker(
        ILogger<Worker> logger,
        IDevicesRepository repo,
        ISnapshotService snapshot,
        IOptions<SnapshotOptions> snapOptions,
        ISnapshotStorage snapsStore,
        IImageAnalyzer analyzer,
        IHealthCheckRepository healthCheckRepo)
    {
        _logger = logger;
        _repo = repo;
        _snapshot = snapshot;
        _snapOptions = snapOptions.Value;
        _snapshotStorage = snapsStore;
        _analyzer = analyzer;
        _healthCheckRepo = healthCheckRepo;
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

                        //The analysis section
                        var metrics = _analyzer.Measure(jpegBytes);

                        _logger.LogInformation(
                            "Analysis d={Device} ch={Channel} blurVar = {LapVar:F1} lumaStd={LumaStd:F1}",
                            d.Name ?? d.Id, ch.Id, metrics.LaplacianVariance, metrics.LumaStdDev);

                        if (_analyzer.IsBlurry(metrics))
                        {
                            _logger.LogWarning("Image is blurry for d={Device} ch={Channel}", d.Name ?? d.Id, ch.Id);
                        }

                        if (_analyzer.IsSingleColor(metrics))
                        {
                            _logger.LogWarning("Image looks single-color for d={Device} ch={Channel}", d.Name ?? d.Id, ch.Id);
                        }

                        // Health Check logging

                        var cam = new HealthCheckCamera
                        {
                            ChannelId = ch.Id,
                            Metrics = metrics,
                            SnapshotInfo = new SnapshotInfo
                            {
                                Path = savedPath,
                                Bytes = jpegBytes.Length
                            }
                        };

                        if (_analyzer.IsBlurry(metrics)) cam.Issues.Add("BLUR");
                        if (_analyzer.IsSingleColor(metrics)) cam.Issues.Add("SINGLE_COLOR");

                        cam.Ok = cam.Issues.Count == 0;

                        var hc = new HealthCheckDoc
                        {
                            DeviceId = d.Id,
                            StartedAt = DateTimeOffset.UtcNow,
                            FinishedAt = DateTimeOffset.UtcNow,
                            Ok = cam.Ok,
                            Summary = new HealthCheckSummary
                            {
                                TotalCameras = d.Channels.Count,
                                Failures = cam.Ok ? 1 : 0
                            },
                            Cameras = new List<HealthCheckCamera> { cam }
                        };

                        await _healthCheckRepo.InsertAsync(hc, stoppingToken);
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
