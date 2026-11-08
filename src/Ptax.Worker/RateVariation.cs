namespace Ptax.Worker;

public record RateVariation(
    string Currency,
    DateOnly PreviousDate,
    DateOnly Date,
    decimal PreviousSellRate,
    decimal SellRate,
    decimal ChangePercent
);