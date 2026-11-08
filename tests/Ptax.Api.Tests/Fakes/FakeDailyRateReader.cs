using Ptax.Data;

namespace Ptax.Api.Tests.Fakes;

public class FakeDailyRateReader(DailyRate? result) : IDailyRateReader
{
    public int Calls { get; private set; }

    public Task<DailyRate?> GetLatestOnOrBeforeAsync(string currency, DateOnly date, CancellationToken cancellationToken = default)
    {
        Calls++;

        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DailyRate>> GetRangeAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();
}