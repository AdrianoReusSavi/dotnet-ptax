using Ptax.Data;

namespace Ptax.Api;

public static class AlertEndpoints
{
    private const int MaxHistoryDays = 366;

    public static IEndpointRouteBuilder MapAlertEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/alerts", GetAlertsAsync);

        return app;
    }

    private static async Task<IResult> GetAlertsAsync(
        string? currency,
        DateOnly? from,
        DateOnly? to,
        IRateAlertReader reader,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        to ??= DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        from ??= to.Value.AddDays(-30);

        string? normalizedCurrency = null;

        var errors = new Dictionary<string, string[]>();

        if (currency is not null && !CurrencyCode.TryNormalize(currency, out normalizedCurrency))
            errors["currency"] = ["Currency must be a 3-letter code."];

        if (from > to)
            errors["from"] = ["'from' must be on or before 'to'."];

        if (to.Value.DayNumber - from.Value.DayNumber + 1 > MaxHistoryDays)
            errors["to"] = [$"The period cannot exceed {MaxHistoryDays} days."];

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var alerts = await reader.GetAlertsAsync(from.Value, to.Value, normalizedCurrency, cancellationToken);

        return Results.Ok(alerts.Select(AlertResponse.From));
    }
}