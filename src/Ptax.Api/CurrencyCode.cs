namespace Ptax.Api;

public static class CurrencyCode
{
    public static bool TryNormalize(string value, out string normalized)
    {
        normalized = string.Empty;

        var normalize = value.ToUpperInvariant();

        if (normalize.Length != 3 || !normalize.All(char.IsAsciiLetterUpper))
            return false;

        normalized = normalize;

        return true;
    }
}