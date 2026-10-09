namespace Game.Core;

/// <summary>
/// Feste Untereinheiten für Mengen in der Simulation. Alle Mengen sind Ganzzahlen (<c>long</c>),
/// damit die Simulation ohne Gleitkomma auskommt (siehe docs/architecture.md, Abschnitt 3.3).
/// </summary>
public static class Quantities
{
    /// <summary>Ressourcenmengen werden in Tausendstel gespeichert: 1 Einheit = 1000.</summary>
    public const long ResourceScale = 1000;

    /// <summary>Geld wird in Cent gespeichert: 1 Geldeinheit = 100.</summary>
    public const long MoneyScale = 100;

    // Feste deutsche Schreibweise für Texte (z. B. Ablehnungsgründe), unabhängig von installierten Kulturen.
    private static readonly System.Globalization.NumberFormatInfo GermanNumbers = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NegativeSign = "-",
    };

    /// <summary>Ressourcenmenge in Tausendstel als Text, z. B. 12500 → „12,5“.</summary>
    public static string FormatResource(long thousandths) =>
        ((decimal)thousandths / ResourceScale).ToString("#,0.###", GermanNumbers);

    /// <summary>Geld in Cent als Text, z. B. 123450 → „1.234,5“.</summary>
    public static string FormatMoney(long cents) =>
        ((decimal)cents / MoneyScale).ToString("#,0.##", GermanNumbers);

    /// <summary>
    /// Rechnet eine Dezimalzahl exakt in die Untereinheit um, z. B. 0.5 Ressourcen → 500.
    /// Gibt <c>null</c> zurück, wenn der Wert mehr Nachkommastellen hat, als die Untereinheit darstellen kann,
    /// oder nicht in einen <c>long</c> passt.
    /// </summary>
    internal static long? ToFixed(decimal value, long scale)
    {
        decimal scaled = value * scale;
        if (scaled != decimal.Truncate(scaled) || scaled > long.MaxValue || scaled < long.MinValue)
        {
            return null;
        }

        return (long)scaled;
    }
}
