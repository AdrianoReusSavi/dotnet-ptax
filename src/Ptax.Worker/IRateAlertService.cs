using Ptax.Data;

namespace Ptax.Worker;

public interface IRateAlertService
{
    Task<IReadOnlyList<RateAlert>> CheckAsync(string currency, DateOnly from, DateOnly to, decimal thresholdPercent, CancellationToken cancellationToken = default);
}