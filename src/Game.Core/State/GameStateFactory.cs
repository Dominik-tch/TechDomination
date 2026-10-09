using Game.Core.Data;
using Game.Core.Determinism;

namespace Game.Core.State;

/// <summary>Erzeugt neue Spielzustände.</summary>
public static class GameStateFactory
{
    /// <summary>
    /// Neuer Zustand bei Tick 0: Start-Besitzer aus der Karte, Startgeld und Startbestände aus economy.json,
    /// Marktpreise auf Basispreis, alle Fabriken ohne Begrenzung, Start-Armeen aus map.json.
    /// Gleiche Daten und gleicher Seed ergeben denselben Zustand.
    /// </summary>
    public static GameState CreateNew(GameData data, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(data);

        var basePrices = data.Resources.Select(r => r.BasePrice).ToArray();
        var nations = data.Nations
            .Select(nation => new NationState(
                nation.Id,
                data.Economy.StartMoney,
                data.Economy.StartResources,
                basePrices,
                new int?[data.Buildings.Count],
                new int[data.Buildings.Count]))
            .ToList();

        var provinces = data.Provinces
            .Select(province => new ProvinceState(province.Id, province.StartOwner, new int[data.Buildings.Count], null, [], 0))
            .ToList();

        var state = new GameState(seed, tick: 0, DeterministicRandom.FromSeed(seed), nations, provinces, [], nextArmyId: 0);

        // Start-Armeen stehen in der Stadt und gehören dem Start-Besitzer (Platzhalter bis zum Spielstart-Setup in M9).
        foreach (var province in data.Provinces.Where(p => p.StartArmy.Any(count => count > 0)))
        {
            state.CreateArmy(province.StartOwner, Map.PathPosition.AtCity(province.Id), province.StartArmy);
        }

        return state;
    }
}
