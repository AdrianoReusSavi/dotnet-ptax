namespace Ptax.Data;

public interface IDailyRateReader
{
    Task<DailyRate?> GetLatestOnOrBeforeAsync(string currency, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DailyRate>> GetRangeAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}