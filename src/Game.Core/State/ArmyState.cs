using System.Text.Json.Serialization;
using Game.Core.Map;

namespace Game.Core.State;

/// <summary>Eine Armee: gemischter Stack aus Einheiten mit einer Position im Wegenetz und einem optionalen Marsch.</summary>
public sealed class ArmyState
{
    private readonly int[] _units;
    private readonly List<MoveLeg> _legs;

    [JsonConstructor]
    internal ArmyState(ArmyId id, NationId owner, PathPosition position, IReadOnlyList<int> units, IReadOnlyList<MoveLeg> legs)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(legs);

        Id = id;
        Owner = owner;
        Position = position;
        _units = units.ToArray();
        _legs = legs.ToList();
    }

    public ArmyId Id { get; }

    public NationId Owner { get; internal set; }

    /// <summary>Aktuelle Position im Wegenetz.</summary>
    public PathPosition Position { get; internal set; }

    /// <summary>Anzahl Einheiten je Typ, Index = <see cref="UnitTypeId.Value"/>.</summary>
    public IReadOnlyList<int> Units => _units;

    /// <summary>Verbleibende Abschnitte des aktuellen Marsches; leer = die Armee steht.</summary>
    public IReadOnlyList<MoveLeg> Legs => _legs;

    public bool IsMoving => _legs.Count > 0;

    public int TotalUnits => _units.Sum();

    public int GetUnits(UnitTypeId type) => _units[type.Value];

    internal void SetRoute(PathPosition start, IEnumerable<MoveLeg> legs)
    {
        Position = start;
        _legs.Clear();
        _legs.AddRange(legs);
    }

    internal void Halt() => _legs.Clear();

    internal void RemoveFirstLeg() => _legs.RemoveAt(0);

    internal void AddUnits(IReadOnlyList<int> units)
    {
        for (int i = 0; i < _units.Length; i++)
        {
            _units[i] += units[i];
        }
    }

    internal void RemoveUnits(IReadOnlyList<int> units)
    {
        for (int i = 0; i < _units.Length; i++)
        {
            _units[i] -= units[i];
        }
    }
}
