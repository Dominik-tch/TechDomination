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
    internal GameState(ulong seed, long tick, DeterministicRandom rng, IReadOnlyList<ProvinceState> provinces)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(provinces);
        ArgumentOutOfRangeException.ThrowIfNegative(tick);

        for (int i = 0; i < provinces.Count; i++)
        {
            if (provinces[i].Id.Value != i)
            {
                throw new ArgumentException("Provinzen müssen lückenlos nach ID sortiert sein.", nameof(provinces));
            }
        }

        Seed = seed;
        Tick = tick;
        Rng = rng;
        Provinces = provinces;
    }

    /// <summary>Seed der Partie. Grundlage für abgeleitete Seeds (z. B. für die KI).</summary>
    public ulong Seed { get; }

    /// <summary>Aktueller Tick. Die einzige Zeiteinheit der Simulation.</summary>
    public long Tick { get; internal set; }

    /// <summary>Zufallsgenerator der Simulation. Nur die Simulation darf ihn verwenden.</summary>
    public DeterministicRandom Rng { get; }

    /// <summary>Zustand aller Provinzen, Index = <see cref="ProvinceId.Value"/>.</summary>
    public IReadOnlyList<ProvinceState> Provinces { get; }

    public ProvinceState GetProvince(ProvinceId id) => Provinces[id.Value];
}
