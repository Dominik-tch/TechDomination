using Game.Core.Determinism;

namespace Game.Core.State;

/// <summary>Erzeugt neue Spielzustände.</summary>
public static class GameStateFactory
{
    /// <summary>Neuer Zustand bei Tick 0. Gleicher Seed ergibt denselben Zustand.</summary>
    public static GameState CreateNew(ulong seed) => new(seed, tick: 0, DeterministicRandom.FromSeed(seed));
}
