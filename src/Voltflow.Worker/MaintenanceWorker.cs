using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Voltflow.Worker;

public sealed class MaintenanceWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MaintenanceWorker> _logger;

    public MaintenanceWorker(IServiceScopeFactory scopeFactory, ILogger<MaintenanceWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        // Initial run
        await RunProcessAsync(stoppingToken);
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunProcessAsync(stoppingToken);
        }
    }

    private async Task RunProcessAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var timeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var processor = scope.ServiceProvider.GetRequiredService<MaintenanceProcessor>();
            var processed = await processor.ProcessDueContractsAsync(timeProvider.GetUtcNow().UtcDateTime, stoppingToken);
            if (processed > 0)
                _logger.LogInformation("Maintenance cycle processed {Count} due contracts.", processed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing maintenance contracts.");
        }
    }
}
