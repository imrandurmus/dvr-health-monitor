using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DvrWorker.Configurations
{
    public class SnapshotStorageOptions
    {
        public string BasePath { get; set; } = Path.Combine(AppContext.BaseDirectory, "DvrSnapshots");

        public int RetentionDays { get; set; } = 7;

    }
}
