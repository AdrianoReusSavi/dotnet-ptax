using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Ptax.Api.Tests.Base;
using Ptax.Data;

namespace Ptax.Api.Tests;

public class RateEndpointsTests(PtaxApiFactory factory) : IClassFixture<PtaxApiFactory>
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
    public async Task GetRate_Exact_Date_Returns_Rate()
    {
        await factory.SeedAsync(
            CreateRate("AAA", new DateOnly(2026, 10, 30), 4m),
            CreateRate("AAA", new DateOnly(2026, 10, 31), 5m));

        var response = await _client.GetAsync("/rates/AAA?date=2026-10-31");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rate = await response.Content.ReadFromJsonAsync<RateResponse>();

        Assert.NotNull(rate);
        Assert.Equal("AAA", rate.Currency);
        Assert.Equal(5m, rate.BuyRate);
        Assert.Equal(5.1m, rate.SellRate);
        Assert.Equal(5.2m, rate.BuyParity);
        Assert.Equal(5.3m, rate.SellParity);
        Assert.Equal(new DateOnly(2026, 10, 31), rate.Date);
    }

    [Fact]
    public async Task GetRate_Date_Without_Rate_Returns_Previous_One_With_Its_Date()
    {
        await factory.SeedAsync(
            CreateRate("BBB", new DateOnly(2026, 10, 30), 4m),
            CreateRate("BBB", new DateOnly(2026, 10, 31), 5m),
            CreateRate("BBB", new DateOnly(2026, 11, 03), 6m));

        var response = await _client.GetAsync("/rates/BBB?date=2026-11-02");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rate = await response.Content.ReadFromJsonAsync<RateResponse>();

        Assert.NotNull(rate);
        Assert.Equal("BBB", rate.Currency);
        Assert.Equal(5m, rate.BuyRate);
        Assert.Equal(5.1m, rate.SellRate);
        Assert.Equal(5.2m, rate.BuyParity);
        Assert.Equal(5.3m, rate.SellParity);
        Assert.Equal(new DateOnly(2026, 10, 31), rate.Date);
    }

    [Fact]
    public async Task GetRate_Without_Date_Uses_Utc_Today()
    {
        await factory.SeedAsync(
            CreateRate("CCC", new DateOnly(2026, 11, 03), 4m),
            CreateRate("CCC", new DateOnly(2026, 11, 04), 5m));

        var response = await _client.GetAsync("/rates/CCC");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rate = await response.Content.ReadFromJsonAsync<RateResponse>();

        Assert.NotNull(rate);
        Assert.Equal("CCC", rate.Currency);
        Assert.Equal(4m, rate.BuyRate);
        Assert.Equal(4.1m, rate.SellRate);
        Assert.Equal(4.2m, rate.BuyParity);
        Assert.Equal(4.3m, rate.SellParity);
        Assert.Equal(new DateOnly(2026, 11, 03), rate.Date);
    }

    [Fact]
    public async Task GetRate_Lowercase_Currency_Is_Accepted()
    {
        await factory.SeedAsync(
            CreateRate("DDD", new DateOnly(2026, 10, 30), 4m));

        var response = await _client.GetAsync("/rates/ddd?date=2026-10-30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var rate = await response.Content.ReadFromJsonAsync<RateResponse>();

        Assert.NotNull(rate);
        Assert.Equal("DDD", rate.Currency);
        Assert.Equal(4m, rate.BuyRate);
        Assert.Equal(4.1m, rate.SellRate);
        Assert.Equal(4.2m, rate.BuyParity);
        Assert.Equal(4.3m, rate.SellParity);
        Assert.Equal(new DateOnly(2026, 10, 30), rate.Date);
    }

    [Theory]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("U1D")]
    public async Task GetRate_Invalid_Currency_Returns_Bad_Request(string currency)
    {
        var response = await _client.GetAsync($"/rates/{currency}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("currency", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetRate_No_Rate_Returns_Not_Found()
    {
        var response = await _client.GetAsync("/rates/EEE");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetRate_Invalid_Date_Returns_Bad_Request()
    {
        var response = await _client.GetAsync("/rates/FFF?date=30-10-2026");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}