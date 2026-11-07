using Microsoft.EntityFrameworkCore;

namespace Ptax.Data;

public class DailyRateRepository(PtaxDbContext context) : IDailyRateRepository, IDailyRateReader
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

    public async Task<DailyRate?> GetLatestOnOrBeforeAsync(string currency, DateOnly date, CancellationToken cancellationToken = default)
        => await context.DailyRates
            .AsNoTracking()
            .Where(rate => rate.Currency == currency && rate.Date <= date)
            .OrderByDescending(rate => rate.Date)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<DailyRate>> GetRangeAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => await context.DailyRates
            .AsNoTracking()
            .Where(rate => rate.Currency == currency && rate.Date >= from && rate.Date <= to)
            .OrderBy(rate => rate.Date)
            .ToListAsync(cancellationToken);
}