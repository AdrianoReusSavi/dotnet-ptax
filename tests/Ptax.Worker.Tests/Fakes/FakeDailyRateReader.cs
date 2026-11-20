using Ptax.Data;

namespace Ptax.Worker.Tests.Fakes;

public class FakeDailyRateReader : IDailyRateReader
{
    public List<DailyRate> DailyRates { get; } = [];
    public string? ReceivedCurrency { get; private set; }
    public DateOnly? ReceivedFrom { get; private set; }
    public DateOnly? ReceivedTo { get; private set; }

    public Task<DailyRate?> GetLatestOnOrBeforeAsync(string currency, DateOnly date, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<IReadOnlyList<DailyRate>> GetRangeAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        ReceivedCurrency = currency;
        ReceivedFrom = from;
        ReceivedTo = to;

        return Task.FromResult((IReadOnlyList<DailyRate>)[.. DailyRates]);
    }
}