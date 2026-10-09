using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Events;
using Game.Core.State;

namespace Game.Core;

/// <summary>Führt die Simulation Tick für Tick aus (siehe docs/architecture.md, Abschnitt 4).</summary>
public static class Simulation
{
    /// <summary>
    /// Führt genau einen Tick aus: Commands in fester Reihenfolge (Nation, dann Sequenz) prüfen und anwenden,
    /// danach den Tick erhöhen. Systeme kommen ab Meilenstein M3 dazu.
    /// </summary>
    /// <param name="commands">Commands für genau diesen Tick; die Eingabereihenfolge spielt keine Rolle.</param>
    /// <returns>Die Events dieses Ticks.</returns>
    public static IReadOnlyList<GameEvent> Step(GameState state, GameData data, IReadOnlyList<CommandEnvelope> commands)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(commands);

        // Vorab prüfen, damit bei einem Fehler nicht ein Teil der Commands schon angewendet ist.
        if (commands.FirstOrDefault(c => c.ExecuteAtTick != state.Tick) is { } misplaced)
        {
            throw new ArgumentException(
                $"Command für Tick {misplaced.ExecuteAtTick} kann nicht in Tick {state.Tick} ausgeführt werden.",
                nameof(commands));
        }

        var events = new List<GameEvent>();
        foreach (var envelope in commands.OrderBy(c => c.Issuer.Value).ThenBy(c => c.Sequence))
        {
            var validation = IsKnownNation(data, envelope.Issuer)
                ? envelope.Command.Validate(state, data, envelope.Issuer)
                : ValidationResult.Invalid("Unbekannte Nation.");

            if (validation.IsValid)
            {
                envelope.Command.Apply(state, data, envelope.Issuer);
            }
            else
            {
                events.Add(new CommandRejected(state.Tick, envelope, validation.Reason!));
            }
        }

        state.Tick++;
        return events;
    }

    private static bool IsKnownNation(GameData data, NationId nation) =>
        nation.Value >= 0 && nation.Value < data.Nations.Count;
}
