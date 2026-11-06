namespace Ptax.Worker.Tests.Fakes;

public class FakePtaxSyncService : IPtaxSyncService
{
    public List<(string Currency, DateOnly From, DateOnly To)> Calls { get; } = [];

    public Dictionary<string, Exception> Failures { get; } = [];

    public Task<int> SyncAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Calls.Add((currency, from, to));

        if (Failures.TryGetValue(currency, out var exception))
            throw exception;

        return Task.FromResult(1);
    }
}