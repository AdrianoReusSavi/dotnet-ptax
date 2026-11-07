using Ptax.Data;

namespace Ptax.Api;

public record RateResponse(
    string Currency,
    DateOnly Date,
    decimal BuyRate,
    decimal SellRate,
    decimal BuyParity,
    decimal SellParity)
{
    public static RateResponse From(DailyRate rate)
        => new(rate.Currency, rate.Date, rate.BuyRate, rate.SellRate, rate.BuyParity, rate.SellParity);
}