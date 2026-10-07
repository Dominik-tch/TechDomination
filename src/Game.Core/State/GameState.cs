using System.Text.Json.Serialization;
using Game.Core.Determinism;

namespace Game.Core.State;

/// <summary>
/// Vollständiger, serialisierbarer Spielzustand. Reine Daten ohne Logik.
/// Setter sind <c>internal</c>: Godot kann nur lesen, Änderungen laufen über die Simulation.
/// </summary>
public sealed class GameState
{
    [JsonConstructor]
    internal GameState(ulong seed, long tick, DeterministicRandom rng)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentOutOfRangeException.ThrowIfNegative(tick);

        Seed = seed;
        Tick = tick;
        Rng = rng;
    }

    /// <summary>Seed der Partie. Grundlage für abgeleitete Seeds (z. B. für die KI).</summary>
    public ulong Seed { get; }

    /// <summary>Aktueller Tick. Die einzige Zeiteinheit der Simulation.</summary>
    public long Tick { get; internal set; }

    /// <summary>Zufallsgenerator der Simulation. Nur die Simulation darf ihn verwenden.</summary>
    public DeterministicRandom Rng { get; }
}
