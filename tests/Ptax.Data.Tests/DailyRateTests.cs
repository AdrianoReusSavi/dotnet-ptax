using Microsoft.EntityFrameworkCore;
using Ptax.Data.Tests.Base;

namespace Ptax.Data.Tests;

public class DailyRateTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private static DailyRate CreateRate(string currency, DateOnly date)
        => new()
            {
                Currency = currency,
                Date = date,
                BuyRate = 6.05050m,
                SellRate = 6.05220m,
                BuyParity = 1.11620m,
                SellParity = 1.11640m
            };

    [Fact]
    public async Task Add_Persists_All_Properties()
    {
        var date = new DateOnly(2026, 10, 30);

        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.DailyRates.Add(CreateRate("EUR", date));

            await writeContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var saved = await readContext.DailyRates.SingleAsync(rate => rate.Currency == "EUR" && rate.Date == date);

        Assert.Equal("EUR", saved.Currency);
        Assert.Equal(date, saved.Date);
        Assert.Equal(6.05050m, saved.BuyRate);
        Assert.Equal(6.05220m, saved.SellRate);
        Assert.Equal(1.11620m, saved.BuyParity);
        Assert.Equal(1.11640m, saved.SellParity);
    }

    [Fact]
    public async Task Add_Same_Currency_And_Date_Throws_DbUpdateException()
    {
        var date = new DateOnly(2026, 10, 31);

        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.DailyRates.Add(CreateRate("EUR", date));

            await writeContext.SaveChangesAsync();
        }

        await using var duplicatedContext = fixture.CreateContext();
        duplicatedContext.DailyRates.Add(CreateRate("EUR", date));

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            duplicatedContext.SaveChangesAsync());
    }

    [Fact]
    public async Task Add_Different_Currencies_On_The_Same_Date_Saves_Both()
    {
        var date = new DateOnly(2026, 11, 1);
        var dailyUsd = CreateRate("USD", date);
        var dailyEur = CreateRate("EUR", date);

        await using (var writeContext = fixture.CreateContext())
        {
            writeContext.DailyRates.Add(dailyUsd);
            writeContext.DailyRates.Add(dailyEur);

            await writeContext.SaveChangesAsync();
        }

        await using var readContext = fixture.CreateContext();
        var saved = await readContext.DailyRates.Where(rate => rate.Date == date).ToListAsync();

        Assert.Collection(saved.OrderBy(o => o.Currency),
            item => Assert.Equal("EUR", item.Currency),
            item => Assert.Equal("USD", item.Currency)
        );
    }
}