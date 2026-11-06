using Microsoft.Extensions.Options;

namespace Ptax.Worker;

public class PtaxSyncWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<PtaxSyncOptions> options,
    TimeProvider timeProvider,
    ILogger<PtaxSyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval, timeProvider);

        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var to = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var from = to.AddDays(-options.Value.WindowDays);

        foreach (var currency in options.Value.Currencies)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var service = scope.ServiceProvider.GetRequiredService<IPtaxSyncService>();

                var saved = await service.SyncAsync(currency, from, to, cancellationToken);

                logger.LogInformation("Synced {Currency}: {Saved} rates from {From} to {To}",
                    currency, saved, from, to);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "Failed to sync {Currency}", currency);
            }
        }
    }
}