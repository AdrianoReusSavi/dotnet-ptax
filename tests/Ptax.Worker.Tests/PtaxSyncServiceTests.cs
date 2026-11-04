using Ptax.Bcb;
using Ptax.Worker.Tests.Fakes;

namespace Ptax.Worker.Tests;

public class PtaxSyncServiceTests
{
    [Fact]
    public async Task Sync_Saves_Only_Closing_Quote()
    {
        var quotedAt = new DateTime(2026, 10, 29);
        var date = new DateOnly(2026, 10, 29);

        PtaxQuote[] quotes = 
        [
            new PtaxQuote(1m, 1.1m, 1.2m, 1.3m, quotedAt, BulletinType.Opening),
            new PtaxQuote(2m, 2.1m, 2.2m, 2.3m, quotedAt, BulletinType.Intermediate),
            new PtaxQuote(3m, 3.1m, 3.2m, 3.3m, quotedAt, BulletinType.Closing),
        ];

        var client = new FakePtaxClient(quotes);
        var repository = new FakeDailyRateRepository();
        var service = new PtaxSyncService(client, repository);

        var saved = await service.SyncAsync("USD", date, date);

        var rate = Assert.Single(repository.DailyRates);

        Assert.Equal(3m, rate.BuyRate);
        Assert.Equal(1, saved);
    }

    [Fact]
    public async Task Sync_Maps_Closing_Quote_To_DailyRate()
    {
        var quotedAt = new DateTime(2026, 10, 29, 13, 10, 0);
        var from = new DateOnly(2026, 10, 23);
        var to = new DateOnly(2026, 10, 30);

        var client = new FakePtaxClient(new PtaxQuote(5m, 5.1m, 5.2m, 5.3m, quotedAt, BulletinType.Closing));
        var repository = new FakeDailyRateRepository();
        var service = new PtaxSyncService(client, repository);

        await service.SyncAsync("EUR", from, to);

        var rate = Assert.Single(repository.DailyRates);

        Assert.Equal("EUR", rate.Currency);
        Assert.Equal(new DateOnly(2026, 10, 29), rate.Date);
        Assert.Equal(5m, rate.BuyRate);
        Assert.Equal(5.1m, rate.SellRate);
        Assert.Equal(5.2m, rate.BuyParity);
        Assert.Equal(5.3m, rate.SellParity);
    }

    [Fact]
    public async Task Sync_Passes_Currency_And_Period_To_Client()
    {
        var client = new FakePtaxClient();
        var repository = new FakeDailyRateRepository();
        var service = new PtaxSyncService(client, repository);

        var from = new DateOnly(2026, 10, 23);
        var to = new DateOnly(2026, 10, 30);

        await service.SyncAsync("EUR", from, to);

        Assert.Equal("EUR", client.ReceivedCurrency);
        Assert.Equal(from, client.ReceivedFrom);
        Assert.Equal(to, client.ReceivedTo);
    }

    [Fact]
    public async Task Sync_No_Quotes_Returns_Zero()
    {
        var client = new FakePtaxClient();
        var repository = new FakeDailyRateRepository();
        var service = new PtaxSyncService(client, repository);

        var date = new DateOnly(2026, 11, 2);

        var saved = await service.SyncAsync("USD", date, date);

        Assert.Equal(0, saved);
        Assert.Empty(repository.DailyRates);
    }

    [Fact]
    public async Task Sync_Client_Failure_Propagates_And_Saves_Nothing()
    {
        var client = new FakePtaxClient { ExceptionToThrow = new HttpRequestException() };
        var repository = new FakeDailyRateRepository();
        var service = new PtaxSyncService(client, repository);

        var date = new DateOnly(2026, 10, 30);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.SyncAsync("USD", date, date));

        Assert.Empty(repository.DailyRates);
    }
}