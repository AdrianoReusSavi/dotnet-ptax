namespace Ptax.Data;

public interface IRateAlertReader
{
    Task<IReadOnlyList<RateAlert>> GetAlertsAsync(DateOnly from, DateOnly to, string? currency, CancellationToken cancellationToken = default);
}