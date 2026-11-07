using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Ptax.Api.Tests.Base;
using Ptax.Data;

namespace Ptax.Api.Tests;

public class HistoryEndpointTests(PtaxApiFactory factory) : IClassFixture<PtaxApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static DailyRate CreateRate(string currency, DateOnly date, decimal value = 6.05050m)
        => new()
            {
                Currency = currency,
                Date = date,
                BuyRate = value,
                SellRate = value + 0.1m,
                BuyParity = value + 0.2m,
                SellParity = value + 0.3m
            };

    [Fact]
    public async Task GetHistory_Returns_Rates_In_Period_Ordered_By_Date()
    {
        await factory.SeedAsync(
            CreateRate("AAA", new DateOnly(2026, 10, 20), 4m),
            CreateRate("AAA", new DateOnly(2026, 10, 15), 5m),
            CreateRate("AAA", new DateOnly(2026, 10, 21), 6m),
            CreateRate("AAA", new DateOnly(2026, 10, 18), 7m),
            CreateRate("AAA", new DateOnly(2026, 10, 19), 8m),
            CreateRate("AAA", new DateOnly(2026, 10, 30), 9m));

        var response = await _client.GetAsync("/rates/aaa/history?from=2026-10-16&to=2026-10-22");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rates = await response.Content.ReadFromJsonAsync<List<RateResponse>>();

        Assert.NotNull(rates);
        Assert.Collection(rates,
            item => Assert.Equal(new DateOnly(2026, 10, 18), item.Date),
            item => Assert.Equal(new DateOnly(2026, 10, 19), item.Date),
            item => Assert.Equal(new DateOnly(2026, 10, 20), item.Date),
            item => Assert.Equal(new DateOnly(2026, 10, 21), item.Date));

        Assert.Equal(new RateResponse("AAA", new DateOnly(2026, 10, 18), 7m, 7.1m, 7.2m, 7.3m), rates[0]);
    }

    [Fact]
    public async Task GetHistory_No_Rates_Returns_Empty_List()
    {
        var response = await _client.GetAsync("/rates/BBB/history?from=2026-10-30&to=2026-10-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rates = await response.Content.ReadFromJsonAsync<List<RateResponse>>();

        Assert.NotNull(rates);
        Assert.Empty(rates);
    }

    [Fact]
    public async Task GetHistory_From_After_To_Returns_Bad_Request()
    {
        var response = await _client.GetAsync("/rates/CCC/history?from=2026-10-31&to=2026-10-30");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("from", problem.Errors.Keys);
    }

    [Theory]
    [InlineData("2026-01-01", "2027-01-01", HttpStatusCode.OK)]
    [InlineData("2026-01-01", "2027-01-02", HttpStatusCode.BadRequest)]
    public async Task GetHistory_Period_Limit(string from, string to, HttpStatusCode expected)
    {
        var response = await _client.GetAsync($"/rates/ddd/history?from={from}&to={to}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_Invalid_Currency_Returns_Bad_Request()
    {
        var response = await _client.GetAsync("/rates/e1e/history?from=2026-10-30&to=2026-10-31");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("currency", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetHistory_Missing_To_Returns_Bad_Request()
    {
        var response = await _client.GetAsync("/rates/FFF/history?from=2026-10-30");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}