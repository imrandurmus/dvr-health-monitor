using DvrWorker.Configurations;
using DvrWorker.Data;
using DvrWorker.Models;
using DvrWorker.Services;
using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DvrWorker.Jobs;

public sealed class HealthCheckJob : IJob
{
    private readonly ILogger<HealthCheckJob> _logger;
    private readonly IDevicesRepository _repo;
    private readonly ISnapshotService _snapshot;
    private readonly SnapshotOptions _snapOptions;
    private readonly ISnapshotStorage _snapshotStorage;
    private readonly IImageAnalyzer _analyzer;
    private readonly IHealthCheckRepository _healthCheckRepo;

    private DateOnly _lastCleanup = DateOnly.MinValue;

    public HealthCheckJob(
        ILogger<HealthCheckJob> logger,
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

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;


        _logger.LogInformation(
            "HealthCheckJob fired at {Now} by trigger {TriggerKey}",
            DateTimeOffset.UtcNow,
            context.Trigger.Key);


        // LISTING DEVICES
        var devices = await _repo.GetEnabledAsync(ct);

        

        if (devices.Count == 0)
        {
            _logger.LogInformation("No enabled devices found.");
            return;
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


        // FETCHING AND SAVING SNAPSHOTS (CURRENTLY FAKE) + DB SAVE
        foreach (var d in devices)
        {
            var startedAt = DateTimeOffset.UtcNow;
            // INTIALIZING THE HEALTHCHECK DOC
            var doc = new HealthCheckDoc
            {
                DeviceId = d.Id,
                StartedAt = startedAt,
                Cameras = new List<HealthCheckCamera>()
            };

            foreach (var ch in d.Channels.Where(c => c.Enabled))
            {
                try
                {
                    // STUB FETCH (CHANGE IT FROM THE SNAPSHOT OPTIONS ;) ) 
                    var jpegBytes = await _snapshot.GetSnapshotAsync(_snapOptions.Url, ct);
                    if (jpegBytes is null || jpegBytes.Length == 0)
                    {
                        _logger.LogWarning("Empty snapshot for {Device}/{Channel}", d.Name ?? d.Id, ch.Id);
                        continue;
                    }

                    //SAVE SNAPSHOT
                    var savedPath = await _snapshotStorage.SaveAsync(
                        d, ch.Id, jpegBytes, DateTimeOffset.UtcNow, ct);
                    _logger.LogInformation("Saved snapshot d={Device} ch={Channel} -> {Path}",
                        d.Name ?? d.Id, ch.Id, savedPath);

                    //ANALYZE SNAPSHOT
                    var metrics = _analyzer.Measure(jpegBytes);
                    _logger.LogInformation(
                        "Analysis d={Device} ch={Channel} blurVar={LapVar:F1} lumaStd={LumaStd:F1}",
                        d.Name ?? d.Id, ch.Id, metrics.LaplacianVariance, metrics.LumaStdDev);

                    if (_analyzer.IsBlurry(metrics))
                        _logger.LogWarning("Image is blurry for d={Device} ch={Channel}", d.Name ?? d.Id, ch.Id);

                    if (_analyzer.IsSingleColor(metrics))
                        _logger.LogWarning("Image looks single-color for d={Device} ch={Channel}", d.Name ?? d.Id, ch.Id);





                    var issues = new List<string>();
                    if (_analyzer.IsBlurry(metrics)) issues.Add("BLUR");
                    if (_analyzer.IsSingleColor(metrics)) issues.Add("SINGLE_COLOR");

                    var cameraCheck = new HealthCheckCamera
                    {
                        ChannelId = ch.Id,
                        Ok = issues.Count == 0,
                        Metrics = metrics,
                        Issues = issues,
                        SnapshotInfo = new SnapshotInfo
                        {
                            Path = savedPath,
                            Bytes = jpegBytes.Length,
                            ContentType = "image/jpeg"
                        }

                    };

                    doc.Cameras.Add(cameraCheck);

                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Snapshot fetch/save failed for {Device}/{Channel}", d.Name ?? d.Id, ch.Id);
                }             
            }
            doc.Summary = new HealthCheckSummary
            {
                TotalCameras = d.Channels.Count,
                Failures = doc.Cameras.Count(c => !c.Ok)
            };
            doc.Ok = doc.Summary.Failures == 0;
            doc.FinishedAt = DateTimeOffset.UtcNow;
            await _healthCheckRepo.InsertAsync(doc, ct);
        }
    }
}
