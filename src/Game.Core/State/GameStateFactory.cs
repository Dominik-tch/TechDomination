using Game.Core.Data;
using Game.Core.Determinism;

namespace Game.Core.State;

/// <summary>Erzeugt neue Spielzustände.</summary>
public static class GameStateFactory
{
    /// <summary>
    /// Neuer Zustand bei Tick 0: Start-Besitzer aus der Karte, Startgeld und Startbestände aus economy.json.
    /// Gleiche Daten und gleicher Seed ergeben denselben Zustand.
    /// </summary>
    public static GameState CreateNew(GameData data, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(data);

        var nations = data.Nations
            .Select(nation => new NationState(nation.Id, data.Economy.StartMoney, data.Economy.StartResources))
            .ToList();

        var provinces = data.Provinces
            .Select(province => new ProvinceState(province.Id, province.StartOwner))
            .ToList();

        return new GameState(seed, tick: 0, DeterministicRandom.FromSeed(seed), nations, provinces);
    }
}
