namespace Ptax.Bcb.Internal;

internal record OlindaResponse(IReadOnlyList<OlindaQuote> Value);

internal record OlindaQuote(
    decimal ParidadeCompra,
    decimal ParidadeVenda,
    decimal CotacaoCompra,
    decimal CotacaoVenda,
    string DataHoraCotacao,
    string TipoBoletim
);