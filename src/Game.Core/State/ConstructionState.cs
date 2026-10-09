using System.Text.Json.Serialization;

namespace Game.Core.State;

/// <summary>Laufende Baustelle in einer Provinz. Die Kosten sind beim Baustart bereits bezahlt.</summary>
public sealed class ConstructionState
{
    [JsonConstructor]
    internal ConstructionState(BuildingId building, int targetLevel, int remainingTicks)
    {
        Building = building;
        TargetLevel = targetLevel;
        RemainingTicks = remainingTicks;
    }

    public BuildingId Building { get; }

    /// <summary>Stufe, die das Gebäude nach Fertigstellung hat.</summary>
    public int TargetLevel { get; }

    /// <summary>Ticks bis zur Fertigstellung.</summary>
    public int RemainingTicks { get; internal set; }
}
