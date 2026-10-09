using System.Text.Json.Serialization;

namespace Game.Core.State;

/// <summary>Veränderlicher Zustand einer Provinz. Die festen Eigenschaften stehen in <see cref="Data.ProvinceDefinition"/>.</summary>
public sealed class ProvinceState
{
    private readonly int[] _buildingLevels;
    private readonly List<UnitTypeId> _trainingQueue;

    [JsonConstructor]
    internal ProvinceState(
        ProvinceId id,
        NationId owner,
        IReadOnlyList<int> buildingLevels,
        ConstructionState? construction,
        IReadOnlyList<UnitTypeId> trainingQueue,
        int trainingRemainingTicks)
    {
        ArgumentNullException.ThrowIfNull(buildingLevels);
        ArgumentNullException.ThrowIfNull(trainingQueue);

        Id = id;
        Owner = owner;
        _buildingLevels = buildingLevels.ToArray();
        Construction = construction;
        _trainingQueue = trainingQueue.ToList();
        TrainingRemainingTicks = trainingRemainingTicks;
    }

    public ProvinceId Id { get; }

    /// <summary>Aktueller Besitzer. Jede Provinz hat immer einen Besitzer.</summary>
    public NationId Owner { get; internal set; }

    /// <summary>Stufe jedes Gebäudetyps (0 = nicht gebaut), Index = <see cref="BuildingId.Value"/>.</summary>
    public IReadOnlyList<int> BuildingLevels => _buildingLevels;

    /// <summary>Laufende Baustelle, oder <c>null</c>. Pro Provinz gibt es höchstens eine.</summary>
    public ConstructionState? Construction { get; internal set; }

    /// <summary>Ausbildungsaufträge, je Eintrag eine Einheit. Nur der erste wird gerade ausgebildet.</summary>
    public IReadOnlyList<UnitTypeId> TrainingQueue => _trainingQueue;

    /// <summary>Ticks, bis die erste Einheit der Schlange fertig ist (0 bei leerer Schlange).</summary>
    public int TrainingRemainingTicks { get; internal set; }

    public int GetBuildingLevel(BuildingId id) => _buildingLevels[id.Value];

    internal void SetBuildingLevel(BuildingId id, int level) => _buildingLevels[id.Value] = level;

    internal void EnqueueTraining(UnitTypeId type, int count) => _trainingQueue.AddRange(Enumerable.Repeat(type, count));

    internal void RemoveTrainingAt(int index) => _trainingQueue.RemoveAt(index);
}
