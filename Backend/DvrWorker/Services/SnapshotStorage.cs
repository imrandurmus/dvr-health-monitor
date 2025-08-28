using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DvrWorker.Configurations;
using DvrWorker.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;

namespace DvrWorker.Services;
public sealed class SnapshotStorage : ISnapshotStorage
{
    private readonly SnapshotStorageOptions _options;
    private readonly ILogger<SnapshotStorage> _logger;

    public SnapshotStorage(IOptions<SnapshotStorageOptions> options, ILogger<SnapshotStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
        Directory.CreateDirectory(_options.BasePath);

    }

    // Saves to: BasePath\Site\Device\Channel\YYYY-MM-DD\
    public async Task<string> SaveAsync(Device device,
        int channelId,
        ReadOnlyMemory<byte> jpegBytes,
        DateTimeOffset when,
        CancellationToken ct)
    {
        var site = San(device.Site); // san sanetizes the strings for use in file paths, turns invalid characters like * ? etc. into _
        var dev = San(device.Name);
        var ch = channelId.ToString();

        var inst = when.UtcDateTime;
        var date = inst.ToString("yyyy-MM-dd");
        //var time = inst.ToString("HHmmssfff");


        // folder : BasePath\Site\Device\Channel\YYYY-MM-DD\
        var folder = Path.Combine(_options.BasePath, site, dev, ch, date);
        Directory.CreateDirectory(folder);


        //file : site_device_channel_yyyy_MM_dd_HHmmss.jgp
        var fileName = $"{site}_{dev}_{ch}_{inst:yyyyMMdd'T'HHmmssfff'Z'}.jpg";
        var fullPath = Path.Combine(folder, fileName);

        await File.WriteAllBytesAsync(fullPath, jpegBytes.ToArray(), ct);
        return fullPath;
    }
    public async Task<int> CleanupOldAsync(CancellationToken ct)
    {
        if (!Directory.Exists(_options.BasePath)) return 0;


        var cutoff = DateTimeOffset.Now.AddDays(-_options.RetentionDays);
        int deleted = 0;


        foreach (var path in Directory.EnumerateFiles(_options.BasePath, "*.jpg", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var info = new FileInfo(path);

                if (info.LastWriteTimeUtc < cutoff.UtcDateTime)
                {
                    info.IsReadOnly = false;
                    info.Delete();
                    deleted++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete path {path}", path);

            }
            await Task.Yield(); // keeps worker responsive, After each iteration, the method politely steps aside →
                                // “hey, if something else is waiting (like logging, shutdown, snapshot analysis),
                                // let them run, then I’ll come back.”
        }

        PruneEmptyDirs(_options.BasePath);
        if (deleted > 0)
        {
            _logger.LogInformation("Snapshot cleanup removed {Count} files older than {Days} days.", deleted, _options.RetentionDays);

            return deleted;
        }

        return deleted;
    }
    
    
    // private helpers 

    // Sanetizer function
    private static string San(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "NA";
        var bad = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(s.Length);

        foreach (var c in s)
            sb.Append(Array.IndexOf(bad, c) >= 0 ? '_' : c);
        return sb.ToString().Trim();
    }
    
    private static void PruneEmptyDirs(string root)
    {
        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
            .OrderByDescending(d => d.Length))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    Directory.Delete(dir, false);
            }
            catch { }
        }
    }
    
}
