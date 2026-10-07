using Game.Core.Data;
using Game.Core.Determinism;

namespace Game.Core.State;

/// <summary>Erzeugt neue Spielzustände.</summary>
public static class GameStateFactory
{
    /// <summary>Neuer Zustand bei Tick 0 mit den Start-Besitzern aus der Karte. Gleiche Daten und gleicher Seed ergeben denselben Zustand.</summary>
    public static GameState CreateNew(GameData data, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(data);

        var provinces = data.Provinces
            .Select(province => new ProvinceState(province.Id, province.StartOwner))
            .ToList();

        return new GameState(seed, tick: 0, DeterministicRandom.FromSeed(seed), provinces);
    }
}
