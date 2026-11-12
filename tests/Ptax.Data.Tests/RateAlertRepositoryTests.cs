using Microsoft.EntityFrameworkCore;
using Ptax.Data.Tests.Base;

namespace Ptax.Data.Tests;

public class RateAlertRepositoryTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private static RateAlert CreateAlert(string currency, DateOnly date, decimal changePercent = 4m)
        => new()
        {
            Currency = currency,
            PreviousDate = date.AddDays(-1),
            Date = date,
            PreviousSellRate = changePercent + 0.1m,
            SellRate = changePercent + 0.2m,
            ChangePercent = changePercent
        };

    private async Task<List<RateAlert>> AddNewAsync(params RateAlert[] alerts)
    {
        await using (var context = fixture.CreateContext())
        {
            var repository = new RateAlertRepository(context);
            var addedAlerts = await repository.AddNewAsync(alerts, CancellationToken.None);

            return [.. addedAlerts];
        }
    }

    [Fact]
    public async Task AddNew_Persists_All_Values()
    {
        var alert = new RateAlert
        {
            Currency = "AAA",
            PreviousDate = new DateOnly(2026, 8, 31),
            Date = new DateOnly(2026, 9, 1),
            PreviousSellRate = 5.01234m,
            SellRate = 5m,
            ChangePercent = -3.85m
        };

        Assert.Single(await AddNewAsync(alert));

        await using var context = fixture.CreateContext();
        var saved = await context.RateAlerts.SingleAsync(alert => alert.Currency == "AAA");

        Assert.Equal("AAA", saved.Currency);
        Assert.Equal(new DateOnly(2026, 8, 31), saved.PreviousDate);
        Assert.Equal(new DateOnly(2026, 9, 1), saved.Date);
        Assert.Equal(5.01234m, saved.PreviousSellRate);
        Assert.Equal(5m, saved.SellRate);
        Assert.Equal(-3.85m, saved.ChangePercent);
    }

    [Fact]
    public async Task AddNew_Existing_Key_Is_Not_Returned_Again()
    {
        var alert = CreateAlert("BBB", new DateOnly(2026, 9, 2), 5m);

        await AddNewAsync(alert);

        Assert.Empty(await AddNewAsync(alert));
    }

    [Fact]
    public async Task AddNew_Existing_Key_Keeps_Original_Values()
    {
        var alert = CreateAlert("CCC", new DateOnly(2026, 9, 3), 5m);
        var duplicatedAlert = CreateAlert("CCC", new DateOnly(2026, 9, 3), 3m);

        Assert.Single(await AddNewAsync(alert));

        Assert.Empty(await AddNewAsync(duplicatedAlert));

        await using var context = fixture.CreateContext();
        var saved = await context.RateAlerts.SingleAsync(alert => alert.Currency == "CCC");

        Assert.Equal(5m, saved.ChangePercent);
    }

    [Fact]
    public async Task AddNew_Mixed_Batch_Returns_Only_New()
    {
        var alert = CreateAlert("DDD", new DateOnly(2026, 9, 4), 5m);
        
        await AddNewAsync(alert);

        var added = await AddNewAsync([CreateAlert("DDD", new DateOnly(2026, 9, 4), 5m), CreateAlert("DDD", new DateOnly(2026, 9, 5), 2m)]);

        Assert.Equal(new DateOnly(2026, 9, 5), Assert.Single(added).Date);
        
        await using var context = fixture.CreateContext();

        Assert.Equal(2, await context.RateAlerts.CountAsync(alert => alert.Currency == "DDD"));
    }

    [Fact]
    public async Task AddNew_Same_Date_Other_Currency_Is_New()
    {
        Assert.Single(await AddNewAsync(CreateAlert("EEE", new DateOnly(2026, 9, 5), 3m)));

        var saved = Assert.Single(await AddNewAsync(CreateAlert("FFF", new DateOnly(2026, 9, 5), 5m)));

        Assert.Equal("FFF", saved.Currency);
    }

    [Fact]
    public async Task AddNew_Empty_Batch_Returns_Empty()
    {
        Assert.Empty(await AddNewAsync([]));
    }
}