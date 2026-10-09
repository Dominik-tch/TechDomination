using System.Text.Json.Serialization;
using Game.Core.Map;

namespace Game.Core.State;

/// <summary>Eine Armee: gemischter Stack aus Einheiten mit einer Position im Wegenetz und einem optionalen Marsch.</summary>
public sealed class ArmyState
{
    /// <summary>Volle Stärke in Basispunkten (100 %).</summary>
    public const int FullStrength = 10_000;

    private readonly int[] _units;
    private readonly int[] _strength;
    private readonly List<MoveLeg> _legs;

    [JsonConstructor]
    internal ArmyState(
        ArmyId id, NationId owner, PathPosition position, IReadOnlyList<int> units, IReadOnlyList<int> strength, IReadOnlyList<MoveLeg> legs)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(strength);
        ArgumentNullException.ThrowIfNull(legs);

        Id = id;
        Owner = owner;
        Position = position;
        _units = units.ToArray();
        _strength = strength.ToArray();
        _legs = legs.ToList();
    }

    public ArmyId Id { get; }

    public NationId Owner { get; internal set; }

    /// <summary>Aktuelle Position im Wegenetz.</summary>
    public PathPosition Position { get; internal set; }

    /// <summary>Anzahl Einheiten je Typ, Index = <see cref="UnitTypeId.Value"/>.</summary>
    public IReadOnlyList<int> Units => _units;

    /// <summary>Stärke je Einheitentyp in Basispunkten (<see cref="FullStrength"/> = 100 %), Index = <see cref="UnitTypeId.Value"/>.</summary>
    public IReadOnlyList<int> Strength => _strength;

    /// <summary>Verbleibende Abschnitte des aktuellen Marsches; leer = die Armee steht.</summary>
    public IReadOnlyList<MoveLeg> Legs => _legs;

    public bool IsMoving => _legs.Count > 0;

    public int TotalUnits => _units.Sum();

    public int GetUnits(UnitTypeId type) => _units[type.Value];

    public int GetStrength(UnitTypeId type) => _strength[type.Value];

    internal void SetRoute(PathPosition start, IEnumerable<MoveLeg> legs)
    {
        Position = start;
        _legs.Clear();
        _legs.AddRange(legs);
    }

    internal void Halt() => _legs.Clear();

    internal void RemoveFirstLeg() => _legs.RemoveAt(0);

    /// <summary>Fügt Einheiten mit voller Stärke hinzu.</summary>
    internal void AddUnits(IReadOnlyList<int> units) => AddUnits(units, Enumerable.Repeat(FullStrength, units.Count).ToArray());

    /// <summary>Fügt Einheiten hinzu; die Stärke je Typ wird nach Anzahl gemittelt.</summary>
    internal void AddUnits(IReadOnlyList<int> units, IReadOnlyList<int> strength)
    {
        for (int i = 0; i < _units.Length; i++)
        {
            int total = _units[i] + units[i];
            if (units[i] > 0)
            {
                _strength[i] = (int)(((long)_units[i] * _strength[i] + (long)units[i] * strength[i]) / total);
            }

            _units[i] = total;
        }
    }

    /// <summary>Entfernt Einheiten; die Stärke der verbleibenden bleibt gleich.</summary>
    internal void RemoveUnits(IReadOnlyList<int> units)
    {
        for (int i = 0; i < _units.Length; i++)
        {
            _units[i] -= units[i];
            if (_units[i] == 0)
            {
                _strength[i] = FullStrength;
            }
        }
    }

    /// <summary>Senkt die Stärke eines Typs; bei 0 lösen sich dessen Einheiten auf.</summary>
    internal void ReduceStrength(UnitTypeId type, int basisPoints)
    {
        _strength[type.Value] -= basisPoints;
        if (_strength[type.Value] <= 0)
        {
            _units[type.Value] = 0;
            _strength[type.Value] = FullStrength;
        }
    }
}
