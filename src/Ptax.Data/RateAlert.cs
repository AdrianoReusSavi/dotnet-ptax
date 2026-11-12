namespace Ptax.Data;

public class RateAlert
{
    public required string Currency { get; init; }
    public required DateOnly PreviousDate { get; init; }
    public required DateOnly Date { get; init; }
    public required decimal PreviousSellRate { get; init; }
    public required decimal SellRate { get; init; }
    public required decimal ChangePercent { get; init; }
}