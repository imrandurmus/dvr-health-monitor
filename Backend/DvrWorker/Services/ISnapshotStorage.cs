using DvrWorker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DvrWorker.Services;
public interface ISnapshotStorage
{
    Task<string> SaveAsync(Device device, int channelId, ReadOnlyMemory<byte> jpegBytes, DateTimeOffset when, CancellationToken ct);
    Task<int>CleanupOldAsync(CancellationToken ct);

        
}

