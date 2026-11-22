using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Ptax.Api.Tests.Base;
using Ptax.Data;

namespace Ptax.Api.Tests;

public class AlertEndpointsTests(PtaxApiFactory factory) : IClassFixture<PtaxApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static RateAlert CreateAlert(string currency, DateOnly date, decimal changePercent = 4m)
        => new()
        {
            Currency = currency,
            PreviousDate = date.AddDays(-1),
            Date = date,
            PreviousSellRate = changePercent + 0.1m,
            SellRate = changePercent + 0.2m,
            ChangePercent = changePercent
        };

    [Fact]
    public async Task GetAlerts_Returns_Alerts_In_Period_Newest_First()
    {
        await factory.SeedAlertsAsync(
            CreateAlert("AAA", new DateOnly(2026, 10, 20), 3m),
            CreateAlert("AAA", new DateOnly(2026, 10, 10), 4m),
            CreateAlert("AAA", new DateOnly(2026, 10, 25), 5m),
            CreateAlert("AAA", new DateOnly(2026, 10, 15), 6m),
            CreateAlert("AAA", new DateOnly(2026, 10, 30), 7m),
            CreateAlert("ZZZ", new DateOnly(2026, 10, 20), 3m)
        );

        var response = await _client.GetAsync("/alerts?currency=aaa&from=2026-10-12&to=2026-10-27");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var alerts = await response.Content.ReadFromJsonAsync<List<AlertResponse>>();

        Assert.NotNull(alerts);
        Assert.Collection(alerts,
            item => Assert.Equal(new DateOnly(2026, 10, 25), item.Date),
            item => Assert.Equal(new DateOnly(2026, 10, 20), item.Date),
            item => Assert.Equal(new DateOnly(2026, 10, 15), item.Date));

        Assert.Equal(new AlertResponse("AAA", new DateOnly(2026, 10, 24), new DateOnly(2026, 10, 25), 5.1m, 5.2m, 5m), alerts[0]);
    }

    [Fact]
    public async Task GetAlerts_Without_Currency_Returns_All_Currencies()
    {
        await factory.SeedAlertsAsync(
            CreateAlert("BBB", new DateOnly(2001, 12, 01), 3m),
            CreateAlert("CCC", new DateOnly(2001, 12, 02), 4m)
        );

        var response = await _client.GetAsync("/alerts?from=2001-12-01&to=2001-12-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var alerts = await response.Content.ReadFromJsonAsync<List<AlertResponse>>();

        Assert.NotNull(alerts);
        Assert.Equal(["CCC", "BBB"], alerts.Select(alert => alert.Currency));
    }

    [Fact]
    public async Task GetAlerts_Defaults_To_Last_30_Days()
    {
        await factory.SeedAlertsAsync(
            CreateAlert("DDD", new DateOnly(2026, 10, 03), 3m),
            CreateAlert("DDD", new DateOnly(2026, 10, 04), 4m),
            CreateAlert("DDD", new DateOnly(2026, 11, 03), 5m),
            CreateAlert("DDD", new DateOnly(2026, 11, 04), 6m)
        );

        var response = await _client.GetAsync("/alerts?currency=DDD");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var alerts = await response.Content.ReadFromJsonAsync<List<AlertResponse>>();

        Assert.NotNull(alerts);
        Assert.Collection(alerts,
            item => Assert.Equal(new DateOnly(2026, 11, 03), item.Date),
            item => Assert.Equal(new DateOnly(2026, 10, 04), item.Date));
    }

    [Fact]
    public async Task GetAlerts_Only_To_Starts_30_Days_Before_It()
    {
        await factory.SeedAlertsAsync(
            CreateAlert("EEE", new DateOnly(2026, 08, 31), 3m),
            CreateAlert("EEE", new DateOnly(2026, 09, 01), 4m),
            CreateAlert("EEE", new DateOnly(2026, 10, 01), 5m)
        );

        var response = await _client.GetAsync("/alerts?currency=EEE&to=2026-10-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var alerts = await response.Content.ReadFromJsonAsync<List<AlertResponse>>();

        Assert.NotNull(alerts);
        Assert.Collection(alerts,
            item => Assert.Equal(new DateOnly(2026, 10, 01), item.Date),
            item => Assert.Equal(new DateOnly(2026, 09, 01), item.Date));
    }

    [Fact]
    public async Task GetAlerts_Invalid_Currency_Returns_Bad_Request()
    {
        var response = await _client.GetAsync("/alerts?currency=US");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("currency", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetAlerts_From_After_To_Returns_Bad_Request()
    {
        var response = await _client.GetAsync("/alerts?from=2026-10-27&to=2026-10-12");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("from", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetAlerts_Period_Over_366_Days_Returns_Bad_Request()
    {
        var response = await _client.GetAsync("/alerts?from=2026-01-01&to=2027-01-02");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("to", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetAlerts_Period_Of_366_Days_Without_Alerts_Returns_Empty_List()
    {
        var response = await _client.GetAsync("/alerts?from=2003-01-01&to=2004-01-01");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var alerts = await response.Content.ReadFromJsonAsync<List<AlertResponse>>();

        Assert.NotNull(alerts);
        Assert.Empty(alerts);
    }
}