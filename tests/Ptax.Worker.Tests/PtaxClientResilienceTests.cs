using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Ptax.Bcb;
using Ptax.Worker.Tests.Fakes;

namespace Ptax.Worker.Tests;

public class PtaxClientResilienceTests
{
    [Fact]
    public async Task AddPtaxClient_Retries_On_Server_Error()
    {
        var handler = new SequenceHttpMessageHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);

        var services = new ServiceCollection();

        services.AddPtaxClient().ConfigurePrimaryHttpMessageHandler(() => handler);

        var client = services.BuildServiceProvider().GetRequiredService<IPtaxClient>();

        var quotes = await client.GetQuotesAsync("USD", new DateOnly(2026, 10, 29), new DateOnly(2026, 10, 30));

        Assert.Empty(quotes);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task AddPtaxClient_Does_Not_Retry_On_Bad_Request()
    {
        var handler = new SequenceHttpMessageHandler(HttpStatusCode.BadRequest);

        var services = new ServiceCollection();

        services.AddPtaxClient().ConfigurePrimaryHttpMessageHandler(() => handler);

        var client = services.BuildServiceProvider().GetRequiredService<IPtaxClient>();

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetQuotesAsync("USD", new DateOnly(2026, 10, 29), new DateOnly(2026, 10, 30)));

        Assert.Equal(1, handler.Calls);
    }
}