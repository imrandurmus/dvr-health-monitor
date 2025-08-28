using System.ComponentModel.DataAnnotations;

namespace DvrWorker.Configurations;

public sealed class SnapshotOptions
{
    [Required, Url]
    public string Url { get; set; } = "https://picsum.photos/200"; // the fake snapshot for day 3 

    [Range(1, 3600)]
    public int IntervalSeconds { get; set; } = 30;   // saving every 30 seconds

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 5;
    
}