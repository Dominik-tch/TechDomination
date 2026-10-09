using Game.Core.Data;
using Game.Core.Events;
using Game.Core.State;

namespace Game.Core.Systems;

/// <summary>Ein Schritt der Simulation, der in jedem Tick nach den Commands läuft (siehe docs/architecture.md, Abschnitt 4.1).</summary>
internal interface ISimulationSystem
{
    /// <param name="events">Hier meldet das System Ereignisse dieses Ticks.</param>
    void Update(GameState state, GameData data, List<GameEvent> events);
}
