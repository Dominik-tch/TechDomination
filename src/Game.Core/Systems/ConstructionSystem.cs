using Game.Core.Data;
using Game.Core.Events;
using Game.Core.State;

namespace Game.Core.Systems;

/// <summary>
/// Baustellen schreiten jeden Tick voran. Eine Baustelle mit Bauzeit N, die in Tick t beginnt,
/// ist am Ende von Tick t + N - 1 fertig, also nach genau N Ticks.
/// </summary>
internal sealed class ConstructionSystem : ISimulationSystem
{
    public void Update(GameState state, GameData data, List<GameEvent> events)
    {
        foreach (var province in state.Provinces)
        {
            if (province.Construction is not { } construction)
            {
                continue;
            }

            construction.RemainingTicks--;
            if (construction.RemainingTicks > 0)
            {
                continue;
            }

            province.SetBuildingLevel(construction.Building, construction.TargetLevel);
            province.Construction = null;
            events.Add(new BuildingCompleted(
                state.Tick, province.Id, construction.Building, construction.TargetLevel, province.Owner));
        }
    }
}
