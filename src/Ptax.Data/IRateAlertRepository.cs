namespace Ptax.Data;

public interface IRateAlertRepository
{
    Task<IReadOnlyList<RateAlert>> AddNewAsync(IReadOnlyCollection<RateAlert> alerts, CancellationToken cancellationToken = default);
}