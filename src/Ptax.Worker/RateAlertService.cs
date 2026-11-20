using Ptax.Data;

namespace Ptax.Worker;

public class RateAlertService(IDailyRateReader reader, IRateAlertRepository repository) : IRateAlertService
{
    public async Task<IReadOnlyList<RateAlert>> CheckAsync(string currency, DateOnly from, DateOnly to, decimal thresholdPercent, CancellationToken cancellationToken = default)
    {
        var dailyRates = await reader.GetRangeAsync(currency, from, to, cancellationToken);

        var rateVariations = VariationDetector.Detect(dailyRates, thresholdPercent);

        var rateAlerts = rateVariations.Select(variation =>
            new RateAlert
            {
                Currency = variation.Currency,
                PreviousDate = variation.PreviousDate,
                Date = variation.Date,
                PreviousSellRate = variation.PreviousSellRate,
                SellRate = variation.SellRate,
                ChangePercent = variation.ChangePercent
            }).ToList();

        return await repository.AddNewAsync(rateAlerts, cancellationToken);
    }
}