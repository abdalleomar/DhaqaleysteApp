using System.Globalization;

namespace DhaqaleysteApp.Services;

/// <summary>Formats prices in the company's currency (from company settings; USD by default).</summary>
public static class PriceFormatter
{
    private static readonly Dictionary<string, string> Symbols = new(StringComparer.OrdinalIgnoreCase)
    {
        ["USD"] = "$",
        ["EUR"] = "€",
        ["GBP"] = "£"
    };

    public static string CurrencyCode { get; set; } = "USD";

    public static string Format(decimal price)
    {
        var amount = price.ToString("#,0.00", CultureInfo.InvariantCulture);
        return Symbols.TryGetValue(CurrencyCode, out var symbol) ? symbol + amount : $"{amount} {CurrencyCode}";
    }
}
