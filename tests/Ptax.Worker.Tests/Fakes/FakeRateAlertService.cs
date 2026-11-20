using Ptax.Data;

namespace Ptax.Worker.Tests.Fakes;

public class FakeRateAlertService : IRateAlertService
{
    public List<(string Currency, DateOnly From, DateOnly To, decimal ThresholdPercent)> Calls { get; } = [];

    public Dictionary<string, List<RateAlert>> Results { get; } = [];

    public Dictionary<string, Exception> Failures { get; } = [];

    public List<string> Events { get; set; } = [];

    public Task<IReadOnlyList<RateAlert>> CheckAsync(string currency, DateOnly from, DateOnly to, decimal thresholdPercent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Calls.Add((currency, from, to, thresholdPercent));

        Events.Add($"alerts:{currency}");

        if (Failures.TryGetValue(currency, out var exception))
            throw exception;

        return Task.FromResult<IReadOnlyList<RateAlert>>(Results.GetValueOrDefault(currency, []));
    }
}