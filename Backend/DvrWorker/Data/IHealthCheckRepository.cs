using DvrWorker.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DvrWorker.Data;

public interface IHealthCheckRepository
{
    Task InsertAsync(HealthCheckDoc doc, CancellationToken ct);
}
