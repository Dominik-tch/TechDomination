using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Economy;

/// <summary>Einnahmen einer Nation pro Wirtschaftstakt.</summary>
/// <param name="Money">Geld in Cent.</param>
/// <param name="Resources">Ressourcen in Tausendstel, Index = <see cref="ResourceId.Value"/>.</param>
public sealed record NationIncome(long Money, IReadOnlyList<long> Resources);

/// <summary>
/// Wirtschaftsregeln, die Systeme und Abfragen gemeinsam nutzen, damit die angezeigten Einnahmen
/// immer genau dem entsprechen, was die Simulation gutschreibt.
/// </summary>
public static class EconomyRules
{
    /// <summary>Wird im Tick <paramref name="tick"/> gutgeschrieben? Das ist am Ende jedes vollen Takts der Fall.</summary>
    public static bool IsEconomyTick(long tick, GameData data) => (tick + 1) % data.Economy.IntervalTicks == 0;

    /// <summary>Menge, die eine Provinz pro Takt von ihrem Rohstoff produziert, in Tausendstel.</summary>
    public static long ProductionPerInterval(GameData data, ProvinceDefinition province) =>
        data.GetResource(province.Resource).ProductionPerInterval;

    /// <summary>Was eine Nation mit ihrem aktuellen Besitz pro Takt einnimmt.</summary>
    public static NationIncome IncomePerInterval(GameState state, GameData data, NationId nation)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(data);

        long money = 0;
        var resources = new long[data.Resources.Count];
        foreach (var province in data.Provinces)
        {
            if (state.GetProvince(province.Id).Owner == nation)
            {
                money += data.Economy.TaxPerProvince;
                resources[province.Resource.Value] += ProductionPerInterval(data, province);
            }
        }

        return new NationIncome(money, resources);
    }
}
