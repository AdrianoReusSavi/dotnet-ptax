using Microsoft.EntityFrameworkCore;
using Npgsql;
using Ptax.Data.Tests.Base;

namespace Ptax.Data.Tests;

public class DailyRateRepositoryTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private static DailyRate CreateRate(string currency, DateOnly date, decimal value = 6.05050m)
        => new()
            {
                Currency = currency,
                Date = date,
                BuyRate = value,
                SellRate = value + 0.1m,
                BuyParity = value + 0.2m,
                SellParity = value + 0.3m
            };

    [Fact]
    public async Task Upsert_New_Key_Persists_All_Values()
    {
        var date = new DateOnly(2026, 9, 1);

        await using (var context = fixture.CreateContext())
        {
            var repository = new DailyRateRepository(context);
            await repository.UpsertAsync([CreateRate("EUR", date, 5m)], CancellationToken.None);
        }

        await using var readContext = fixture.CreateContext();
        var saved = await readContext.DailyRates.SingleAsync(rate => rate.Currency == "EUR" && rate.Date == date);

        Assert.Equal("EUR", saved.Currency);
        Assert.Equal(date, saved.Date);
        Assert.Equal(5m, saved.BuyRate);
        Assert.Equal(5.1m, saved.SellRate);
        Assert.Equal(5.2m, saved.BuyParity);
        Assert.Equal(5.3m, saved.SellParity);
    }

    [Fact]
    public async Task Upsert_Existing_Key_Updates_All_Values()
    {
        var date = new DateOnly(2026, 9, 2);

        await using (var firstContext = fixture.CreateContext())
        {
            var repository = new DailyRateRepository(firstContext);
            await repository.UpsertAsync([CreateRate("USD", date, 5m)], CancellationToken.None);
        }

        await using (var secondContext = fixture.CreateContext())
        {
            var repository = new DailyRateRepository(secondContext);
            await repository.UpsertAsync([CreateRate("USD", date, 7m)], CancellationToken.None);
        }

        await using var readContext = fixture.CreateContext();
        var saved = await readContext.DailyRates.SingleAsync(rate => rate.Date == date);

        Assert.Equal(7m, saved.BuyRate);
        Assert.Equal(7.1m, saved.SellRate);
        Assert.Equal(7.2m, saved.BuyParity);
        Assert.Equal(7.3m, saved.SellParity);
    }

    [Fact]
    public async Task Upsert_New_Key_And_Existing_Key_Add_New_And_Update_Existing()
    {
        var date = new DateOnly(2026, 9, 3);

        await using (var firstContext = fixture.CreateContext())
        {
            var repository = new DailyRateRepository(firstContext);
            await repository.UpsertAsync([CreateRate("USD", date, 3m)], CancellationToken.None);
        }

        await using (var secondContext = fixture.CreateContext())
        {
            var repository = new DailyRateRepository(secondContext);
            await repository.UpsertAsync([CreateRate("USD", date, 7m), CreateRate("EUR", date, 5m)], CancellationToken.None);
        }

        await using var readContext = fixture.CreateContext();
        var saved = await readContext.DailyRates.Where(rate => rate.Date == date).ToListAsync();

        Assert.Collection(saved.OrderBy(o => o.Currency),
            item =>
            {
                Assert.Equal("EUR", item.Currency);
                Assert.Equal(5m, item.BuyRate);
            },
            item =>
            {
                Assert.Equal("USD", item.Currency);
                Assert.Equal(7m, item.BuyRate);
            }
        );
    }

    [Fact]
    public async Task Upsert_Failing_Row_Rolls_Back_Whole_Batch()
    {
        var date = new DateOnly(2026, 9, 4);

        await using (var firstContext = fixture.CreateContext())
        {
            var repository = new DailyRateRepository(firstContext);
            await repository.UpsertAsync([CreateRate("USD", date, 5m)], CancellationToken.None);
        }

        await using (var secondContext = fixture.CreateContext())
        {
            var repository = new DailyRateRepository(secondContext);

            await Assert.ThrowsAsync<PostgresException>(() =>
                repository.UpsertAsync([CreateRate("USD", date, 7m), CreateRate("USDX", date)], CancellationToken.None)
            );
        }

        await using var readContext = fixture.CreateContext();
        var saved = await readContext.DailyRates.SingleAsync(rate => rate.Date == date);

        Assert.Equal(5m, saved.BuyRate);
        Assert.Equal(5.1m, saved.SellRate);
        Assert.Equal(5.2m, saved.BuyParity);
        Assert.Equal(5.3m, saved.SellParity);
    }

    [Fact]
    public async Task Upsert_Empty_Collection_Saves_Nothing()
    {
        var date = new DateOnly(2026, 9, 5);

        await using (var firstContext = fixture.CreateContext())
        {
            var repository = new DailyRateRepository(firstContext);
            await repository.UpsertAsync([], CancellationToken.None);
        }

        await using var readContext = fixture.CreateContext();
        var anySaved = await readContext.DailyRates.AnyAsync(rate => rate.Date == date);

        Assert.False(anySaved);
    }
}