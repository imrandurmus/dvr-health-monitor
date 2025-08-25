namespace DvrWorker.Configurations;

public sealed class SnapshotOptions
{
    public string Url { get; set; } = "https//picsum.photos/200"; // the fake snapshot for day 3 

    public int IntervalSeconds { get; set; } = 30;   // saving every 30 seconds

    public string OutputDir { get; set; } = "Snapshots"; // Local folder
}