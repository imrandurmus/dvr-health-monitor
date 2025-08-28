using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DvrWorker.Models;

public sealed class SnapshotDoc
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DeviceId { get; set; } = default!;
    public int ChannelId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Path { get; set; } = default!;
    public List<ImageInspectionLog> Inspections { get; set; } = new();
}
public sealed class ImageInspectionLog
{
    public DateTime AtUtc { get; set; } = DateTime.UtcNow;
    public double LaplacianVariance { get; set; }
    public double LumaStdDev { get; set; }
    public bool IsBlurry { get; set; }
    public bool IsSingleColor { get; set; }
    public string Analyzer { get; set; } = "image-quality";
    public string Version { get; set; } = "v1";
}