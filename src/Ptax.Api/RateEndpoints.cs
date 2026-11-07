using Ptax.Data;

namespace Ptax.Api;

public static class RateEndpoints
{
    private const int MaxHistoryDays = 366;

    public static IEndpointRouteBuilder MapRateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rates");

        group.MapGet("/{currency}", GetRateAsync);

        group.MapGet("/{currency}/history", GetHistoryAsync);

        return app;
    }

    private static async Task<IResult> GetRateAsync(
        string currency,
        DateOnly? date,
        IDailyRateReader reader,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!CurrencyCode.TryNormalize(currency, out var normalizedCurrency))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["currency"] = ["Currency must be a 3-letter code."]
            });

        var day = date ?? DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

        var rate = await reader.GetLatestOnOrBeforeAsync(normalizedCurrency, day, cancellationToken);

        if (rate is null)
            return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: $"No PTAX rate for {normalizedCurrency} on or before {day:yyyy-MM-dd}.");

        return Results.Ok(RateResponse.From(rate));
    }

    private static async Task<IResult> GetHistoryAsync(
        string currency,
        DateOnly from,
        DateOnly to,
        IDailyRateReader reader,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (!CurrencyCode.TryNormalize(currency, out var normalizedCurrency))
            errors["currency"] = ["Currency must be a 3-letter code."];

        if (from > to)
            errors["from"] = ["'from' must be on or before 'to'."];

        if (to.DayNumber - from.DayNumber + 1 > MaxHistoryDays)
            errors["to"] = [$"The period cannot exceed {MaxHistoryDays} days."];

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var rates = await reader.GetRangeAsync(normalizedCurrency, from, to, cancellationToken);

        return Results.Ok(rates.Select(RateResponse.From));
    }
}