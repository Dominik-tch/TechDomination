using System.Text.Json.Serialization;

namespace Game.Core.State;

/// <summary>Veränderlicher Zustand einer Provinz. Die festen Eigenschaften stehen in <see cref="Data.ProvinceDefinition"/>.</summary>
public sealed class ProvinceState
{
    private readonly int[] _buildingLevels;

    [JsonConstructor]
    internal ProvinceState(ProvinceId id, NationId owner, IReadOnlyList<int> buildingLevels, ConstructionState? construction)
    {
        ArgumentNullException.ThrowIfNull(buildingLevels);

        Id = id;
        Owner = owner;
        _buildingLevels = buildingLevels.ToArray();
        Construction = construction;
    }

    public ProvinceId Id { get; }

    /// <summary>Aktueller Besitzer. Jede Provinz hat immer einen Besitzer.</summary>
    public NationId Owner { get; internal set; }

    /// <summary>Stufe jedes Gebäudetyps (0 = nicht gebaut), Index = <see cref="BuildingId.Value"/>.</summary>
    public IReadOnlyList<int> BuildingLevels => _buildingLevels;

    /// <summary>Laufende Baustelle, oder <c>null</c>. Pro Provinz gibt es höchstens eine.</summary>
    public ConstructionState? Construction { get; internal set; }

    public int GetBuildingLevel(BuildingId id) => _buildingLevels[id.Value];

    internal void SetBuildingLevel(BuildingId id, int level) => _buildingLevels[id.Value] = level;
}
