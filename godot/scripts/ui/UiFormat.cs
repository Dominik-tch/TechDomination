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

    /// <summary>Echtzeit bis zum Ablauf von <paramref name="ticks"/> bei der aktuellen Geschwindigkeit, als „m:ss“.</summary>
    public static string Duration(long ticks, GameSession session)
    {
        long seconds = (ticks + session.Speed.TicksPerSecond - 1) / session.Speed.TicksPerSecond;
        return $"{seconds / 60}:{seconds % 60:00}";
    }

    /// <summary>Kosten als Text, z. B. „300 Geld, 30 Holz, 20 Stahl“.</summary>
    public static string Costs(long money, IReadOnlyList<long> resources, Game.Core.Data.GameData data)
    {
        var parts = new List<string> { $"{Money(money)} Geld" };
        foreach (var resource in data.Resources)
        {
            long amount = resources[resource.Id.Value];
            if (amount > 0)
            {
                parts.Add($"{Resource(amount)} {resource.Name}");
            }
        }

        return string.Join(", ", parts);
    }

    /// <summary>Rezept als Text, z. B. „2 Stahl + 5 Holz → 1 Schienen“.</summary>
    public static string Recipe(Game.Core.Data.RecipeDefinition recipe, Game.Core.Data.GameData data)
    {
        var inputs = data.Resources
            .Where(r => recipe.Inputs[r.Id.Value] > 0)
            .Select(r => $"{Resource(recipe.Inputs[r.Id.Value])} {r.Name}");
        return $"{string.Join(" + ", inputs)} → {Resource(recipe.OutputAmount)} {data.GetResource(recipe.Output).Name}";
    }

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
