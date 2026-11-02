namespace Ptax.Data;

public interface IDailyRateRepository
{
    Task UpsertAsync(IReadOnlyCollection<DailyRate> rates, CancellationToken cancellationToken = default);
}