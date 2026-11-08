using Ptax.Data;

namespace Ptax.Worker.Tests;

public class VariationDetectorTests
{
    private static DailyRate CreateRate(DateOnly date, decimal sellRate) => new()
    {
        Currency = "USD",
        Date = date,
        BuyRate = 1m,
        SellRate = sellRate,
        BuyParity = 1m,
        SellParity = 1m,
    };

    [Fact]
    public void Rise_Above_Threshold_Returns_Variation()
    {
        var variations = VariationDetector.Detect(
            [CreateRate(new(2026, 11, 3), 5m), CreateRate(new(2026, 11, 4), 5.2m)], 2m);

        var variation = Assert.Single(variations);

        Assert.Equal(new RateVariation("USD", new(2026, 11, 3), new(2026, 11, 4), 5m, 5.2m, 4m), variation);
    }

    [Fact]
    public void Change_Below_Threshold_Returns_Nothing()
    {
        var variations = VariationDetector.Detect(
            [CreateRate(new(2026, 11, 3), 5m), CreateRate(new(2026, 11, 4), 5.05m)], 2m);

        Assert.Empty(variations);
    }

    [Fact]
    public void Change_Equal_To_Threshold_Returns_Variation()
    {
        var variations = VariationDetector.Detect(
            [CreateRate(new(2026, 11, 3), 5m), CreateRate(new(2026, 11, 4), 5.1m)], 2m);

        var variation = Assert.Single(variations);

        Assert.Equal(new RateVariation("USD", new(2026, 11, 3), new(2026, 11, 4), 5m, 5.1m, 2m), variation);
    }

    [Fact]
    public void Drop_Above_Threshold_Returns_Negative_Rounded_Change()
    {
        var variations = VariationDetector.Detect(
            [CreateRate(new(2026, 11, 3), 5.2m), CreateRate(new(2026, 11, 4), 5m)], 2m);

        var variation = Assert.Single(variations);

        Assert.Equal(new RateVariation("USD", new(2026, 11, 3), new(2026, 11, 4), 5.2m, 5m, -3.85m), variation);
    }

    [Fact]
    public void Compares_With_Previous_Available_Date()
    {
        var variations = VariationDetector.Detect(
            [CreateRate(new(2026, 10, 29), 5m), CreateRate(new(2026, 10, 30), 5.01m), CreateRate(new(2026, 11, 3), 5.2m)], 2m);

        var variation = Assert.Single(variations);

        Assert.Equal(new RateVariation("USD", new(2026, 10, 30), new(2026, 11, 3), 5.01m, 5.2m, 3.79m), variation);
    }

    [Fact]
    public void Unordered_Input_Is_Sorted_By_Date()
    {
        var variations = VariationDetector.Detect(
            [CreateRate(new(2026, 11, 4), 5.2m), CreateRate(new(2026, 11, 3), 5m)], 2m);

        var variation = Assert.Single(variations);

        Assert.Equal(new RateVariation("USD", new(2026, 11, 3), new(2026, 11, 4), 5m, 5.2m, 4m), variation);
    }

    [Fact]
    public void Single_Rate_Returns_Nothing()
    {
        var variations = VariationDetector.Detect(
            [CreateRate(new(2026, 11, 3), 5m)], 2m);

        Assert.Empty(variations);
    }

    [Fact]
    public void Empty_Input_Returns_Nothing()
    {
        var variations = VariationDetector.Detect(
            [], 2m);

        Assert.Empty(variations);
    }
}