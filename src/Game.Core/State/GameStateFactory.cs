using Game.Core.Data;
using Game.Core.Determinism;

namespace Game.Core.State;

/// <summary>Erzeugt neue Spielzustände.</summary>
public static class GameStateFactory
{
    /// <summary>
    /// Neuer Zustand bei Tick 0: Start-Besitzer aus der Karte, Startgeld und Startbestände aus economy.json,
    /// Marktpreise auf Basispreis, alle Fabriken ohne Begrenzung.
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
            .Select(province => new ProvinceState(province.Id, province.StartOwner, new int[data.Buildings.Count], null))
            .ToList();

        return new GameState(seed, tick: 0, DeterministicRandom.FromSeed(seed), nations, provinces);
    }
}
