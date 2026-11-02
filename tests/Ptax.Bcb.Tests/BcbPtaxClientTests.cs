using System.Net;
using Ptax.Bcb.Tests.Fakes;

namespace Ptax.Bcb.Tests;

public class BcbPtaxClientTests
{
    private static readonly DateOnly From = new(2026, 10, 29);
    private static readonly DateOnly To = new(2026, 10, 30);

    private static string JsonWith(
        string dataHora = "2026-10-29 10:10:54.921",
        string tipoBoletim = "Abertura") => $$"""
        {
          "value": [
            {
              "paridadeCompra": 1.11270,
              "paridadeVenda": 1.11280,
              "cotacaoCompra": 5.95120,
              "cotacaoVenda": 5.95240,
              "dataHoraCotacao": "{{dataHora}}",
              "tipoBoletim": "{{tipoBoletim}}"
            }
          ]
        }
        """;

    private static (BcbPtaxClient Client, FakeHttpMessageHandler Handler) CreateClient(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new FakeHttpMessageHandler(status, json);
        return (new BcbPtaxClient(new HttpClient(handler)), handler);
    }

    [Fact]
    public async Task GetQuotesAsync_Sends_Currency_And_Dates_In_The_Query()
    {
        var (client, handler) = CreateClient(JsonWith());

        await client.GetQuotesAsync("EUR", From, To);

        var query = handler.LastRequest!.RequestUri!.Query;

        Assert.Contains("@moeda='EUR'", query);
        Assert.Contains("@dataInicial='10-29-2026'", query);
        Assert.Contains("@dataFinalCotacao='10-30-2026'", query);
    }

    [Fact]
    public async Task GetQuotesAsync_Maps_All_Fields()
    {
        var (client, _) = CreateClient(JsonWith());

        var quote = Assert.Single(await client.GetQuotesAsync("EUR", From, To));

        var expected = new PtaxQuote(
            5.95120m,
            5.95240m,
            1.11270m,
            1.11280m,
            new DateTime(2026, 10, 29, 10, 10, 54, 921),
            BulletinType.Opening
        );

        Assert.Equal(expected, quote);
    }

    [Theory]
    [InlineData("2026-10-29 10:10:54.921", 9210000)]
    [InlineData("2026-10-29 10:10:54.921123", 9211230)]
    public async Task GetQuotesAsync_Parses_QuotedAt_With_Variable_Fraction_Digits(string dataHora, long ticks)
    {
        var (client, _) = CreateClient(JsonWith(dataHora));

        var quote = Assert.Single(await client.GetQuotesAsync("EUR", From, To));

        Assert.Equal(new DateTime(2026, 10, 29, 10, 10, 54).AddTicks(ticks), quote.QuotedAt);
        Assert.Equal(DateTimeKind.Unspecified, quote.QuotedAt.Kind);
    }

    [Theory]
    [InlineData("Abertura", BulletinType.Opening)]
    [InlineData("Intermediário", BulletinType.Intermediate)]
    [InlineData("Fechamento", BulletinType.Closing)]
    public async Task GetQuotesAsync_Converts_Bulletin_Into_Enum_BulletinType(string bulletin, BulletinType bulletinType)
    {
        var (client, _) = CreateClient(JsonWith(tipoBoletim: bulletin));

        var quote = Assert.Single(await client.GetQuotesAsync("EUR", From, To));

        Assert.Equal(bulletinType, quote.Bulletin);
    }

    [Fact]
    public async Task GetQuotesAsync_Returns_Empty_List_When_Api_Has_No_Quotes()
    {
        var (client, _) = CreateClient("""{ "value": [] }""");

        var quotes = await client.GetQuotesAsync("EUR", From, To);

        Assert.Empty(quotes);
    }

    [Fact]
    public async Task GetQuotesAsync_Throws_HttpRequestException_On_Server_Error()
    {
        var (client, _) = CreateClient("", HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
            await client.GetQuotesAsync("EUR", From, To)
        );
    }

    [Fact]
    public async Task GetQuotesAsync_Throws_FormatException_For_Unknown_Bulletin()
    {
        var (client, _) = CreateClient(JsonWith(tipoBoletim: "Fechamento PTAX"));

        await Assert.ThrowsAsync<FormatException>(async () =>
            await client.GetQuotesAsync("EUR", From, To)
        );
    }
}