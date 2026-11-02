namespace Ptax.Bcb;

public interface IPtaxClient
{
    Task<IReadOnlyList<PtaxQuote>> GetQuotesAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}