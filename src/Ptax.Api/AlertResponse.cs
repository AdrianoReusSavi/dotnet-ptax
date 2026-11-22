using Ptax.Data;

namespace Ptax.Api;

public record AlertResponse(
    string Currency,
    DateOnly PreviousDate,
    DateOnly Date,
    decimal PreviousSellRate,
    decimal SellRate,
    decimal ChangePercent
)
{
    public static AlertResponse From(RateAlert alert)
        => new(alert.Currency, alert.PreviousDate, alert.Date, alert.PreviousSellRate, alert.SellRate, alert.ChangePercent);
}