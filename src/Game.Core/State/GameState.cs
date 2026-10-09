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
    internal GameState(
        ulong seed,
        long tick,
        DeterministicRandom rng,
        IReadOnlyList<NationState> nations,
        IReadOnlyList<ProvinceState> provinces)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(nations);
        ArgumentNullException.ThrowIfNull(provinces);
        ArgumentOutOfRangeException.ThrowIfNegative(tick);

        RequireSortedById(nations, n => n.Id.Value, nameof(nations));
        RequireSortedById(provinces, p => p.Id.Value, nameof(provinces));

        Seed = seed;
        Tick = tick;
        Rng = rng;
        Nations = nations;
        Provinces = provinces;
    }

    /// <summary>Seed der Partie. Grundlage für abgeleitete Seeds (z. B. für die KI).</summary>
    public ulong Seed { get; }

    /// <summary>Aktueller Tick. Die einzige Zeiteinheit der Simulation.</summary>
    public long Tick { get; internal set; }

    /// <summary>Zufallsgenerator der Simulation. Nur die Simulation darf ihn verwenden.</summary>
    public DeterministicRandom Rng { get; }

    /// <summary>Zustand aller Nationen, Index = <see cref="NationId.Value"/>.</summary>
    public IReadOnlyList<NationState> Nations { get; }

    /// <summary>Zustand aller Provinzen, Index = <see cref="ProvinceId.Value"/>.</summary>
    public IReadOnlyList<ProvinceState> Provinces { get; }

    public NationState GetNation(NationId id) => Nations[id.Value];

    public ProvinceState GetProvince(ProvinceId id) => Provinces[id.Value];

    private static void RequireSortedById<T>(IReadOnlyList<T> items, Func<T, int> id, string paramName)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (id(items[i]) != i)
            {
                throw new ArgumentException("Einträge müssen lückenlos nach ID sortiert sein.", paramName);
            }
        }
    }
}
