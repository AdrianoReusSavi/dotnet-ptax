using Ptax.Data.Tests.Base;

namespace Ptax.Data.Tests;

public class RateAlertReaderTests(PostgreSqlFixture fixture) : IClassFixture<PostgreSqlFixture>
{
    private static RateAlert CreateAlert(string currency, DateOnly date, decimal changePercent = 1.5m)
        => new()
        {
            Currency = currency,
            PreviousDate = date.AddDays(-1),
            Date = date,
            PreviousSellRate = 5m,
            SellRate = 5.1m,
            ChangePercent = changePercent
        };

    private async Task SeedAsync(params RateAlert[] alerts)
    {
        await using var context = fixture.CreateContext();
        await new RateAlertRepository(context).AddNewAsync(alerts);
    }

    private async Task<IReadOnlyList<RateAlert>> GetAlertsAsync(DateOnly from, DateOnly to, string? currency)
    {
        await using var context = fixture.CreateContext();
        return await new RateAlertRepository(context).GetAlertsAsync(from, to, currency);
    }

    [Fact]
    public async Task GetAlerts_Returns_Only_Alerts_In_Period()
    {
        await SeedAsync(
            CreateAlert("AAA", new DateOnly(2026, 10, 28)),
            CreateAlert("AAA", new DateOnly(2026, 10, 29)),
            CreateAlert("AAA", new DateOnly(2026, 11, 03)),
            CreateAlert("AAA", new DateOnly(2026, 11, 04)));

        var alerts = await GetAlertsAsync(new DateOnly(2026, 10, 29), new DateOnly(2026, 11, 03), "AAA");

        Assert.Collection(alerts,
            alert => Assert.Equal(new DateOnly(2026, 11, 03), alert.Date),
            alert => Assert.Equal(new DateOnly(2026, 10, 29), alert.Date));
    }

    [Fact]
    public async Task GetAlerts_Filters_By_Currency()
    {
        await SeedAsync(
            CreateAlert("BBB", new DateOnly(2026, 10, 29)),
            CreateAlert("CCC", new DateOnly(2026, 10, 29)));

        var alerts = await GetAlertsAsync(new DateOnly(2026, 10, 29), new DateOnly(2026, 10, 29), "BBB");

        var alert = Assert.Single(alerts);

        Assert.Equal("BBB", alert.Currency);
    }

    [Fact]
    public async Task GetAlerts_Without_Currency_Returns_All_Currencies()
    {
        await SeedAsync(
            CreateAlert("DDD", new DateOnly(2001, 12, 01)),
            CreateAlert("EEE", new DateOnly(2001, 12, 01)));

        var alerts = await GetAlertsAsync(new DateOnly(2001, 11, 30), new DateOnly(2001, 12, 02), null);

        Assert.Equal(["DDD", "EEE"], alerts.Select(a => a.Currency));
    }

    [Fact]
    public async Task GetAlerts_Orders_Newest_First_Then_By_Currency()
    {
        await SeedAsync(
            CreateAlert("GGG", new DateOnly(2026, 11, 30)),
            CreateAlert("GGG", new DateOnly(2026, 11, 29)),
            CreateAlert("FFF", new DateOnly(2026, 11, 29)));

        var alerts = await GetAlertsAsync(new DateOnly(2026, 11, 29), new DateOnly(2026, 11, 30), null);

        Assert.Collection(alerts,
            alert => Assert.Equal(("GGG", new DateOnly(2026, 11, 30)), (alert.Currency, alert.Date)),
            alert => Assert.Equal(("FFF", new DateOnly(2026, 11, 29)), (alert.Currency, alert.Date)),
            alert => Assert.Equal(("GGG", new DateOnly(2026, 11, 29)), (alert.Currency, alert.Date)));
    }

    [Fact]
    public async Task GetAlerts_No_Alerts_Returns_Empty()
    {
        await SeedAsync(
            CreateAlert("HHH", new DateOnly(2026, 10, 30)));

        var alerts = await GetAlertsAsync(new DateOnly(2026, 10, 01), new DateOnly(2026, 10, 15), "HHH");

        Assert.Empty(alerts);
    }
}