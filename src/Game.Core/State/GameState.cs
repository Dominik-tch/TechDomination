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
        IReadOnlyList<ProvinceState> provinces,
        IReadOnlyList<ArmyState> armies,
        int nextArmyId)
    {
        ArgumentNullException.ThrowIfNull(rng);
        ArgumentNullException.ThrowIfNull(nations);
        ArgumentNullException.ThrowIfNull(provinces);
        ArgumentNullException.ThrowIfNull(armies);
        ArgumentOutOfRangeException.ThrowIfNegative(tick);

        RequireSortedById(nations, n => n.Id.Value, nameof(nations));
        RequireSortedById(provinces, p => p.Id.Value, nameof(provinces));
        for (int i = 0; i < armies.Count; i++)
        {
            bool ascending = i == 0 || armies[i].Id.Value > armies[i - 1].Id.Value;
            if (!ascending || armies[i].Id.Value >= nextArmyId)
            {
                throw new ArgumentException("Armeen müssen aufsteigend nach ID sortiert sein und unter 'nextArmyId' liegen.", nameof(armies));
            }
        }

        Seed = seed;
        Tick = tick;
        Rng = rng;
        Nations = nations;
        Provinces = provinces;
        _armies = armies.ToList();
        NextArmyId = nextArmyId;
    }

    private readonly List<ArmyState> _armies;

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

    /// <summary>Alle Armeen, aufsteigend nach ID.</summary>
    public IReadOnlyList<ArmyState> Armies => _armies;

    /// <summary>ID, die die nächste neue Armee bekommt.</summary>
    public int NextArmyId { get; private set; }

    public NationState GetNation(NationId id) => Nations[id.Value];

    /// <summary>Die Armee mit dieser ID, oder <c>null</c>, wenn es sie nicht (mehr) gibt.</summary>
    public ArmyState? FindArmy(ArmyId id)
    {
        int low = 0;
        int high = _armies.Count - 1;
        while (low <= high)
        {
            int middle = (low + high) / 2;
            int value = _armies[middle].Id.Value;
            if (value == id.Value)
            {
                return _armies[middle];
            }

            if (value < id.Value)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }

    /// <summary>Entfernt eine Armee, z. B. nach dem Zusammenführen.</summary>
    internal void RemoveArmy(ArmyId id) => _armies.RemoveAll(army => army.Id == id);

    /// <summary>Legt eine neue Armee mit der nächsten freien ID an.</summary>
    internal ArmyState CreateArmy(NationId owner, Map.PathPosition position, IReadOnlyList<int> units)
    {
        var army = new ArmyState(new ArmyId(NextArmyId++), owner, position, units, []);
        _armies.Add(army);
        return army;
    }

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
