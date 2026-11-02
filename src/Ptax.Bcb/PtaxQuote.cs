namespace Ptax.Bcb;

public record PtaxQuote(
    decimal BuyRate,
    decimal SellRate,
    decimal BuyParity,
    decimal SellParity,
    DateTime QuotedAt,
    BulletinType Bulletin
);