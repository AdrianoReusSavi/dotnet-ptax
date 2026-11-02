namespace Ptax.Data;

public class DailyRate
{
    public required string Currency { get; init; }
    public required DateOnly Date { get; init; }
    public required decimal BuyRate { get; init; }
    public required decimal SellRate { get; init; }
    public required decimal BuyParity { get; init; }
    public required decimal SellParity { get; init; }
}