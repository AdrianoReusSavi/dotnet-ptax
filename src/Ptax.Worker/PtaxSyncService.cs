using Ptax.Bcb;
using Ptax.Data;

namespace Ptax.Worker;

public class PtaxSyncService(IPtaxClient client, IDailyRateRepository repository) : IPtaxSyncService
{
    public async Task<int> SyncAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var quotes = await client.GetQuotesAsync(currency, from, to, cancellationToken);

        var rates = quotes
            .Where(quote => quote.Bulletin == BulletinType.Closing)
            .Select(quote => new DailyRate
            {
                Currency = currency,
                Date = DateOnly.FromDateTime(quote.QuotedAt),
                BuyRate = quote.BuyRate,
                SellRate = quote.SellRate,
                BuyParity = quote.BuyParity,
                SellParity = quote.SellParity,
            })
            .ToList();

        await repository.UpsertAsync(rates, cancellationToken);

        return rates.Count;
    }
}