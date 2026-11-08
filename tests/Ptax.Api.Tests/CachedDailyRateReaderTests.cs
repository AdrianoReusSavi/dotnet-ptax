using Microsoft.Extensions.Logging.Abstractions;
using Ptax.Api.Tests.Fakes;
using Ptax.Data;

namespace Ptax.Api.Tests;

public class CachedDailyRateReaderTests
{
    private static readonly DateOnly Date = new(2026, 11, 3);

    private static DailyRate CreateRate(DateOnly date) => new()
    {
        Currency = "USD",
        Date = date,
        BuyRate = 5.1m,
        SellRate = 5.2m,
        BuyParity = 1m,
        SellParity = 1.1m,
    };

    private static CachedDailyRateReader CreateReader(FakeDailyRateReader inner, FakeDistributedCache cache)
        => new(inner, cache, NullLogger<CachedDailyRateReader>.Instance);

    [Fact]
    public async Task Second_Call_Is_Served_From_Cache()
    {
        var inner = new FakeDailyRateReader(CreateRate(Date));
        var reader = CreateReader(inner, new FakeDistributedCache());

        await reader.GetLatestOnOrBeforeAsync("USD", Date);

        var rate = await reader.GetLatestOnOrBeforeAsync("USD", Date);

        Assert.Equal(1, inner.Calls);
        Assert.NotNull(rate);
        Assert.Equal(5.1m, rate.BuyRate);
        Assert.Equal(1.1m, rate.SellParity);
        Assert.Equal(Date, rate.Date);
    }

    [Fact]
    public async Task Different_Currency_Or_Date_Is_Not_Served_From_Same_Entry()
    {
        var inner = new FakeDailyRateReader(CreateRate(Date));
        var reader = CreateReader(inner, new FakeDistributedCache());

        await reader.GetLatestOnOrBeforeAsync("USD", Date);
        await reader.GetLatestOnOrBeforeAsync("EUR", Date);
        await reader.GetLatestOnOrBeforeAsync("USD", new(2026, 11, 4));

        Assert.Equal(3, inner.Calls);
    }

    [Fact]
    public async Task Exact_Date_Is_Cached_For_One_Day()
    {
        var cache = new FakeDistributedCache();
        var reader = CreateReader(new FakeDailyRateReader(CreateRate(Date)), cache);

        await reader.GetLatestOnOrBeforeAsync("USD", Date);

        var options = Assert.Single(cache.Options).Value;

        Assert.Equal(TimeSpan.FromDays(1), options.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task Previous_Date_Is_Cached_For_Five_Minutes()
    {
        var cache = new FakeDistributedCache();
        var reader = CreateReader(new FakeDailyRateReader(CreateRate(Date)), cache);

        await reader.GetLatestOnOrBeforeAsync("USD", new(2026, 11, 4));

        var options = Assert.Single(cache.Options).Value;

        Assert.Equal(TimeSpan.FromMinutes(5), options.AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public async Task Not_Found_Is_Not_Cached()
    {
        var inner = new FakeDailyRateReader(null);
        var cache = new FakeDistributedCache();
        var reader = CreateReader(inner, cache);

        var first = await reader.GetLatestOnOrBeforeAsync("USD", Date);
        var second = await reader.GetLatestOnOrBeforeAsync("USD", Date);

        Assert.Null(first);
        Assert.Null(second);
        Assert.Empty(cache.Entries);
        Assert.Equal(2, inner.Calls);
    }

    [Fact]
    public async Task Cache_Failure_Falls_Back_To_Database()
    {
        var inner = new FakeDailyRateReader(CreateRate(Date));
        var reader = CreateReader(inner, new FakeDistributedCache { ExceptionToThrow = new InvalidOperationException() });

        await reader.GetLatestOnOrBeforeAsync("USD", Date);

        var rate = await reader.GetLatestOnOrBeforeAsync("USD", Date);

        Assert.Equal(2, inner.Calls);
        Assert.NotNull(rate);
        Assert.Equal(5.1m, rate.BuyRate);
        Assert.Equal(1.1m, rate.SellParity);
        Assert.Equal(Date, rate.Date);
    }

    [Fact]
    public async Task Cancellation_Is_Not_Swallowed()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var inner = new FakeDailyRateReader(CreateRate(Date));
        var reader = CreateReader(inner, new FakeDistributedCache());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            reader.GetLatestOnOrBeforeAsync("USD", Date, cts.Token));
    }
}