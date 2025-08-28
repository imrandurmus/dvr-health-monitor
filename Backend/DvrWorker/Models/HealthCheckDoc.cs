using DvrWorker.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DvrWorker.Models;


public sealed class HealthCheckDoc
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string DeviceId { get; set; } = default!;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset FinishedAt { get; set; }
    public bool Ok { get; set; }

    public HealthCheckSummary Summary { get; set; } = new();
    public List<HealthCheckCamera> Cameras { get; set; } = new();

}

public sealed class HealthCheckSummary
{
    public int TotalCameras { get; set; }
    public int Failures { get; set; }
}

public sealed class HealthCheckCamera
{
    public int ChannelId { get; set; }
    public bool Ok { get; set; }

    public ImageMetrics Metrics { get; set; } = default!;
    public List<string> Issues { get; set; } = new();

    public SnapshotInfo SnapshotInfo { get; set; } = new();
}

public sealed class SnapshotInfo
{
    public string Path { get; set; } = default!;
    public long Bytes {  get; set; }
    public string ContentType { get; set; } = "image/jpeg";
}
