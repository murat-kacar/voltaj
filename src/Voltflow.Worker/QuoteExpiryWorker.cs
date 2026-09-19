using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Voltflow.Worker;

public sealed class QuoteExpiryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<QuoteExpiryWorker> _logger;

    public QuoteExpiryWorker(IServiceScopeFactory scopeFactory, ILogger<QuoteExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        await RunAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunAsync(stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
            var processor = scope.ServiceProvider.GetRequiredService<QuoteExpiryProcessor>();
            var expired = await processor.ExpireDueAsync(clock.GetUtcNow().UtcDateTime, stoppingToken);
            if (expired > 0)
                _logger.LogInformation("Quote expiry sweep expired {Count} quotes.", expired);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error expiring quotes.");
        }
    }
}
