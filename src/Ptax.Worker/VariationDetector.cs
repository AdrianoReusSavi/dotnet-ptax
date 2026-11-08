using Ptax.Data;

namespace Ptax.Worker;

public static class VariationDetector
{
    public static IReadOnlyList<RateVariation> Detect(IEnumerable<DailyRate> rates, decimal thresholdPercent)
    {
        var orderedRates = rates.OrderBy(rate => rate.Date).ToList();

        var variations = new List<RateVariation>();

        for (var i = 1; i < orderedRates.Count; i++)
        {
            var previous = orderedRates[i - 1];
            var current = orderedRates[i];

            var variation = (current.SellRate - previous.SellRate) / previous.SellRate * 100;

            if (Math.Abs(variation) >= thresholdPercent)
                variations.Add(new RateVariation(
                    current.Currency,
                    previous.Date,
                    current.Date,
                    previous.SellRate,
                    current.SellRate,
                    Math.Round(variation, 2)));
        }

        return variations;
    }
}