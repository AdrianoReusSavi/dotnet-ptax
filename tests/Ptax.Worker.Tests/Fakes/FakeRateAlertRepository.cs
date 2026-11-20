using Ptax.Data;

namespace Ptax.Worker.Tests.Fakes;

public class FakeRateAlertRepository : IRateAlertRepository
{
    public List<RateAlert> Received { get; } = [];
    public IReadOnlyList<RateAlert>? Result { get; set; }

    public Task<IReadOnlyList<RateAlert>> AddNewAsync(IReadOnlyCollection<RateAlert> alerts, CancellationToken cancellationToken = default)
    {
        Received.AddRange(alerts);

        return Task.FromResult(Result ?? [..alerts]);
    }
}