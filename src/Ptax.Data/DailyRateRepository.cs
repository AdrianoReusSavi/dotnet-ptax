using Microsoft.EntityFrameworkCore;

namespace Ptax.Data;

public class DailyRateRepository(PtaxDbContext context) : IDailyRateRepository
{
    public async Task UpsertAsync(IReadOnlyCollection<DailyRate> rates, CancellationToken cancellationToken = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        foreach (var rate in rates)
        {
            await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO daily_rates (
                    currency, date, buy_rate, sell_rate, buy_parity, sell_parity
                )
                VALUES (
                    {rate.Currency}, {rate.Date}, {rate.BuyRate}, {rate.SellRate}, {rate.BuyParity}, {rate.SellParity}
                )
                ON CONFLICT (currency, date) DO UPDATE SET
                    buy_rate = excluded.buy_rate,
                    sell_rate = excluded.sell_rate,
                    buy_parity = excluded.buy_parity,
                    sell_parity = excluded.sell_parity;
            """, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}