using Ptax.Bcb;

namespace Ptax.Worker.Tests.Fakes;

public class FakePtaxClient(params PtaxQuote[] quotes) : IPtaxClient
{
    public Exception? ExceptionToThrow { get; init; }

    public string? ReceivedCurrency { get; private set; }
    public DateOnly? ReceivedFrom { get; private set; }
    public DateOnly? ReceivedTo { get; private set; }

    public Task<IReadOnlyList<PtaxQuote>> GetQuotesAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        ReceivedCurrency = currency;
        ReceivedFrom = from;
        ReceivedTo = to;

        if (ExceptionToThrow is not null)
            throw ExceptionToThrow;

        return Task.FromResult<IReadOnlyList<PtaxQuote>>(quotes);
    }
}