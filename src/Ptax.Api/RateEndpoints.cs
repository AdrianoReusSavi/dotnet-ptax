using Ptax.Data;

namespace Ptax.Api;

public static class RateEndpoints
{
    public static IEndpointRouteBuilder MapRateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/rates");

        group.MapGet("/{currency}", GetRateAsync);

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
}