using Ptax.Data;
using Ptax.Worker.Tests.Fakes;

namespace Ptax.Worker.Tests;

public class RateAlertServiceTests
{
    private static DailyRate CreateRate(DateOnly date, decimal sellRate) => new()
    {
        Currency = "EUR",
        Date = date,
        BuyRate = 1m,
        SellRate = sellRate,
        BuyParity = 1m,
        SellParity = 1m,
    };

    [Fact]
    public async Task CheckAsync_Passes_Currency_And_Period_To_Reader()
    {
        var reader = new FakeDailyRateReader();
        var repository = new FakeRateAlertRepository();
        var service = new RateAlertService(reader, repository);

        await service.CheckAsync("EUR", new DateOnly(2026, 11, 02), new DateOnly(2026, 11, 03), 2);

        Assert.Equal("EUR", reader.ReceivedCurrency);
        Assert.Equal(new DateOnly(2026, 11, 02), reader.ReceivedFrom);
        Assert.Equal(new DateOnly(2026, 11, 03), reader.ReceivedTo);
    }

    [Fact]
    public async Task CheckAsync_Maps_Variation_To_Alert()
    {
        var reader = new FakeDailyRateReader();
        var repository = new FakeRateAlertRepository();
        var service = new RateAlertService(reader, repository);

        reader.DailyRates.AddRange(CreateRate(new DateOnly(2026, 11, 02), 5m), CreateRate(new DateOnly(2026, 11, 03), 5.1m));

        await service.CheckAsync("EUR", new DateOnly(2026, 11, 02), new DateOnly(2026, 11, 03), 1);

        var alert = Assert.Single(repository.Received);

        Assert.Equal("EUR", alert.Currency);
        Assert.Equal(new DateOnly(2026, 11, 02), alert.PreviousDate);
        Assert.Equal(new DateOnly(2026, 11, 03), alert.Date);
        Assert.Equal(5m, alert.PreviousSellRate);
        Assert.Equal(5.1m, alert.SellRate);
        Assert.Equal(2m, alert.ChangePercent);
    }

    [Fact]
    public async Task CheckAsync_Ignores_Variation_Below_Threshold()
    {
        var reader = new FakeDailyRateReader();
        var repository = new FakeRateAlertRepository();
        var service = new RateAlertService(reader, repository);

        reader.DailyRates.AddRange(CreateRate(new DateOnly(2026, 11, 02), 5m), CreateRate(new DateOnly(2026, 11, 03), 5.1m));

        await service.CheckAsync("EUR", new DateOnly(2026, 11, 02), new DateOnly(2026, 11, 03), 3);

        Assert.Empty(repository.Received);
    }

    [Fact]
    public async Task CheckAsync_Returns_Repository_Result()
    {
        var reader = new FakeDailyRateReader();
        var repository = new FakeRateAlertRepository();
        var service = new RateAlertService(reader, repository);

        reader.DailyRates.AddRange(CreateRate(new DateOnly(2026, 11, 02), 5m), CreateRate(new DateOnly(2026, 11, 03), 5.1m));

        repository.Result = [
            new RateAlert{
                Currency = "USD",
                PreviousDate = new DateOnly(2026, 10, 30),
                Date = new DateOnly(2026, 10, 31),
                PreviousSellRate = 5.3m,
                SellRate = 5.5m,
                ChangePercent = 3.77m}];

        var result = await service.CheckAsync("EUR", new DateOnly(2026, 11, 02), new DateOnly(2026, 11, 03), 1);

        Assert.Same(repository.Result, result);
    }
}