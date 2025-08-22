using DvrWorker.Data;

namespace DvrWorker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDevicesRepository _repo;

    public Worker(ILogger<Worker> logger , IDevicesRepository repo)
    {
        _logger = logger;
        _repo = repo;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _repo.SeedIfEmptyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            
            var devices = await _repo.GetEnabledAsync(stoppingToken);

            if (devices.Count == 0)
            {
                _logger.LogInformation("No enabled devices found.");
            }
            else
            {
                _logger.LogInformation("Found {n} devices", devices.Count);
                foreach (var d in devices)
                {
                    _logger.LogInformation("Camera {Name} {Ip} - {Count} channels",
                        d.Name, d.Ip, d.Channels.Count);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(0.5), stoppingToken);
        }
    }
}
