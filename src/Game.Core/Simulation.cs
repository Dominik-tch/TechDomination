using Game.Core.Data;
using Game.Core.State;

namespace Game.Core;

/// <summary>Führt die Simulation Tick für Tick aus (siehe docs/architecture.md, Abschnitt 4).</summary>
public static class Simulation
{
    /// <summary>
    /// Führt genau einen Tick aus. Bisher wird nur der Tick erhöht;
    /// Commands und Systeme kommen ab Meilenstein M2 dazu.
    /// </summary>
    public static void Step(GameState state, GameData data)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(data);

        state.Tick++;
    }
}
