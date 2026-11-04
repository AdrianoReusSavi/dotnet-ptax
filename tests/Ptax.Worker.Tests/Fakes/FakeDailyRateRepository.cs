using Ptax.Data;

namespace Ptax.Worker.Tests.Fakes;

public class FakeDailyRateRepository : IDailyRateRepository
{
    public List<DailyRate> DailyRates { get; } = [];

    public Task UpsertAsync(IReadOnlyCollection<DailyRate> rates, CancellationToken cancellationToken = default)
    {
        DailyRates.AddRange(rates);

        return Task.CompletedTask;
    }
}