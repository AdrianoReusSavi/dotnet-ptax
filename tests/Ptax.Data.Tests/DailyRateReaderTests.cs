using Ptax.Data.Tests.Base;

namespace Ptax.Data.Tests;

public class DailyRateReaderTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
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

    private async Task SeedAsync(params DailyRate[] rates)
    {
        await using var context = fixture.CreateContext();
        await new DailyRateRepository(context).UpsertAsync(rates);
    }

    [Fact]
    public async Task GetLatestOnOrBefore_Exact_Date_Returns_That_Rate()
    {
        var date = new DateOnly(2026, 10, 1);

        await SeedAsync(
            CreateRate("AAA", new DateOnly(2026, 9, 30), 4m),
            CreateRate("AAA", date, 5m));

        await using var readContext = fixture.CreateContext();
        var saved = await new DailyRateRepository(readContext).GetLatestOnOrBeforeAsync("AAA", date);

        Assert.NotNull(saved);
        Assert.Equal(5m, saved.BuyRate);
        Assert.Equal(date, saved.Date);
    }

    [Fact]
    public async Task GetLatestOnOrBefore_Date_Without_Rate_Returns_Previous_One()
    {
        await SeedAsync(
            CreateRate("BBB", new DateOnly(2026, 10, 30), 4m),
            CreateRate("BBB", new DateOnly(2026, 10, 31), 5m),
            CreateRate("BBB", new DateOnly(2026, 11, 03), 6m));

        await using var readContext = fixture.CreateContext();
        var saved = await new DailyRateRepository(readContext).GetLatestOnOrBeforeAsync("BBB", new DateOnly(2026, 11, 2));

        Assert.NotNull(saved);
        Assert.Equal(5m, saved.BuyRate);
        Assert.Equal(new DateOnly(2026, 10, 31), saved.Date);
    }

    [Fact]
    public async Task GetLatestOnOrBefore_Only_Later_Rates_Returns_Null()
    {
        var date = new DateOnly(2026, 10, 30);

        await SeedAsync(
            CreateRate("CCC", new DateOnly(2026, 10, 31), 4m),
            CreateRate("CCC", date, 5m));

        await using var readContext = fixture.CreateContext();
        var saved = await new DailyRateRepository(readContext).GetLatestOnOrBeforeAsync("CCC", date.AddDays(-1));

        Assert.Null(saved);
    }

    [Fact]
    public async Task GetLatestOnOrBefore_Ignores_Other_Currencies()
    {
        var date = new DateOnly(2026, 10, 30);

        await SeedAsync(CreateRate("DDD", date, 4m));

        await using var readContext = fixture.CreateContext();
        var saved = await new DailyRateRepository(readContext).GetLatestOnOrBeforeAsync("EEE", date);

        Assert.Null(saved);
    }

    [Fact]
    public async Task GetRange_Returns_Only_Rates_In_Period_Ordered_By_Date()
    {
        await SeedAsync(
            CreateRate("FFF", new DateOnly(2026, 10, 28), 4m),
            CreateRate("FFF", new DateOnly(2026, 10, 29), 5m),
            CreateRate("FFF", new DateOnly(2026, 10, 30), 6m),
            CreateRate("FFF", new DateOnly(2026, 11, 03), 7m),
            CreateRate("FFF", new DateOnly(2026, 11, 04), 8m),
            CreateRate("GGG", new DateOnly(2026, 10, 30), 9m));

        await using var readContext = fixture.CreateContext();
        var saves = await new DailyRateRepository(readContext).GetRangeAsync("FFF", new DateOnly(2026, 10, 29), new DateOnly(2026, 11, 03));

        Assert.Collection(saves,
            item => Assert.Equal(new DateOnly(2026, 10, 29), item.Date),
            item => Assert.Equal(new DateOnly(2026, 10, 30), item.Date),
            item => Assert.Equal(new DateOnly(2026, 11, 03), item.Date));
    }

    [Fact]
    public async Task GetRange_No_Rates_Returns_Empty()
    {
        await SeedAsync(
            CreateRate("HHH", new DateOnly(2026, 10, 30), 4m),
            CreateRate("HHH", new DateOnly(2026, 10, 31), 5m));

        await using var readContext = fixture.CreateContext();
        var saves = await new DailyRateRepository(readContext).GetRangeAsync("HHH", new DateOnly(2026, 10, 01), new DateOnly(2026, 10, 15));

        Assert.Empty(saves);
    }
}