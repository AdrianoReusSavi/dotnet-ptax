using Microsoft.EntityFrameworkCore;

namespace Ptax.Data;

public class RateAlertRepository(PtaxDbContext context) : IRateAlertRepository
{
    public async Task<IReadOnlyList<RateAlert>> AddNewAsync(IReadOnlyCollection<RateAlert> alerts, CancellationToken cancellationToken = default)
    {
        var added = new List<RateAlert>();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        foreach (var alert in alerts)
        {
            var rows = await context.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO rate_alerts (
                    currency, previous_date, date, previous_sell_rate, sell_rate, change_percent
                )
                VALUES (
                    {alert.Currency}, {alert.PreviousDate}, {alert.Date}, {alert.PreviousSellRate}, {alert.SellRate}, {alert.ChangePercent}
                )
                ON CONFLICT (currency, date) DO NOTHING
            """, cancellationToken);

            if (rows == 1)
                added.Add(alert);
        }

        await transaction.CommitAsync(cancellationToken);

        return added;
    }
}