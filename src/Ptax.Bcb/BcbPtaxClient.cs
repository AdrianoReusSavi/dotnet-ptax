using System.Globalization;
using System.Net.Http.Json;
using Ptax.Bcb.Internal;

namespace Ptax.Bcb;

public class BcbPtaxClient(HttpClient httpClient) : IPtaxClient
{
    private const string QuotesPath = "https://olinda.bcb.gov.br/olinda/servico/PTAX/versao/v1/odata/CotacaoMoedaPeriodo(moeda=@moeda,dataInicial=@dataInicial,dataFinalCotacao=@dataFinalCotacao)";

    public async Task<IReadOnlyList<PtaxQuote>> GetQuotesAsync(string currency, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
    {
        var dataInicial = from.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);
        var dataFinal = to.ToString("MM-dd-yyyy", CultureInfo.InvariantCulture);

        var url = $"{QuotesPath}?@moeda='{currency}'&@dataInicial='{dataInicial}'&@dataFinalCotacao='{dataFinal}'&$format=json";

        var response = await httpClient.GetFromJsonAsync<OlindaResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("The PTAX API returned an empty response");

        return response.Value.Select(quote => new PtaxQuote(
            quote.CotacaoCompra,
            quote.CotacaoVenda,
            quote.ParidadeCompra,
            quote.ParidadeVenda,
            DateTime.ParseExact(quote.DataHoraCotacao, "yyyy-MM-dd HH:mm:ss.FFFFFF", CultureInfo.InvariantCulture),
            GetBulletin(quote.TipoBoletim)
        )).ToList();
    }

    private static BulletinType GetBulletin(string bulletin) => bulletin switch
    {
        "Abertura" => BulletinType.Opening,
        "Intermediário" => BulletinType.Intermediate,
        "Fechamento" => BulletinType.Closing,
        _ => throw new FormatException($"Unknown PTAX bulletin type: '{bulletin}'.")
    };
}