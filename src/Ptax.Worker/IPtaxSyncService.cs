namespace Ptax.Worker;

public interface IPtaxSyncService
{
    Task<int> SyncAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}