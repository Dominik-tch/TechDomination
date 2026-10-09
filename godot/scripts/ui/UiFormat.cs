using System.Globalization;
using Game.Core;
using Game.Core.Session;

namespace TechDomination.UI;

/// <summary>Formatiert Mengen aus Core (Ganzzahlen in Untereinheiten) für die Anzeige.</summary>
public static class UiFormat
{
    // Feste deutsche Schreibweise, unabhängig von installierten Kulturen.
    private static readonly NumberFormatInfo Numbers = new()
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NegativeSign = "-",
    };

    /// <summary>Ressourcenmenge in Tausendstel, z. B. 12500 → „12,5“.</summary>
    public static string Resource(long thousandths) =>
        ((decimal)thousandths / Quantities.ResourceScale).ToString("#,0.#", Numbers);

    /// <summary>Geld in Cent, z. B. 123450 → „1.234,5“.</summary>
    public static string Money(long cents) =>
        ((decimal)cents / Quantities.MoneyScale).ToString("#,0.##", Numbers);

    /// <summary>Wert mit Vorzeichen, z. B. „+12,5“.</summary>
    public static string Signed(string formatted, long value) => value >= 0 ? $"+{formatted}" : formatted;

    /// <summary>Spielzeit als „h:mm:ss“, gerechnet bei Standardgeschwindigkeit.</summary>
    public static string PlayTime(long ticks, Game.Core.Data.GameData data)
    {
        var time = TimeSpan.FromSeconds(ticks / data.DefaultSpeedLevel.TicksPerSecond);
        return $"{(int)time.TotalHours}:{time.Minutes:00}:{time.Seconds:00}";
    }

    /// <summary>Rechnet eine Menge pro Wirtschaftstakt in eine Menge pro Minute Echtzeit bei der aktuellen Geschwindigkeit um.</summary>
    public static long PerMinute(long perInterval, GameSession session) =>
        perInterval * session.Speed.TicksPerSecond * 60 / session.Data.Economy.IntervalTicks;
}
