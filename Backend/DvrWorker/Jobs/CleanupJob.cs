using DvrWorker.Services;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DvrWorker.Models;

public sealed class CleanupJob : IJob
{
    private readonly ILogger<CleanupJob> _logger;
    private readonly ISnapshotStorage _snapshotStorage;

    public CleanupJob(ILogger<CleanupJob> logger, ISnapshotStorage snapshotStorage)
    {
        _logger = logger;
        _snapshotStorage = snapshotStorage;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        _logger.LogInformation(
            "CleanupJob fired at {Now} by trigger {TriggerKey}",
            DateTimeOffset.UtcNow,
            context.Trigger.Key);

        try
        {
            var deleted = await _snapshotStorage.CleanupOldAsync(ct);
            if (deleted > 0)
            {
                _logger.LogInformation("Cleanup removed {Count} old snapshot", deleted);
            }
            else
            {
                _logger.LogInformation("Cleanup ran - no files removed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cleanup job failed");
        }
    }
}
