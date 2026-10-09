using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Map;
using Game.Core.Military;
using Game.Core.State;

namespace Game.Core.Systems;

/// <summary>
/// Marschierende Armeen rücken jeden Tick um ihr Tempo (langsamste Einheit) auf ihren Abschnitten vor.
/// Erreicht eine Armee einen Haltepunkt, geht sie mit dem restlichen Tempo des Ticks direkt zum nächsten Abschnitt über.
/// Kommt sie am Ziel dort an, wo eine eigene stehende Armee steht, werden beide zusammengeführt.
/// </summary>
internal sealed class MovementSystem : ISimulationSystem
{
    public void Update(GameState state, GameData data, List<GameEvent> events)
    {
        // Kopie, weil beim Zusammenführen Armeen entfernt werden. Reihenfolge nach ID → deterministisch.
        foreach (var army in state.Armies.ToList())
        {
            if (!army.IsMoving)
            {
                continue;
            }

            Advance(army, data);
            if (!army.IsMoving)
            {
                MergeIntoStandingArmy(state, data, army, events);
            }
        }
    }

    private static void Advance(ArmyState army, GameData data)
    {
        long budget = ArmyRules.Speed(data, army);
        while (budget > 0 && army.IsMoving)
        {
            var leg = army.Legs[0];
            var position = army.Position;

            // In einer Stadt beginnt der nächste Abschnitt.
            if (position.IsAtCity)
            {
                position = new PathPosition(leg.From, leg.To, 0);
            }

            long step = Math.Min(budget, leg.StopAt - position.Progress);
            position = position with { Progress = position.Progress + step };
            budget -= step;

            if (position.Progress >= leg.StopAt)
            {
                army.RemoveFirstLeg();
                position = data.Graph.Normalize(position);
            }

            army.Position = position;
        }
    }

    private static void MergeIntoStandingArmy(GameState state, GameData data, ArmyState arrived, List<GameEvent> events)
    {
        var standing = state.Armies.FirstOrDefault(other =>
            other.Id != arrived.Id
            && other.Owner == arrived.Owner
            && !other.IsMoving
            && data.Graph.IsSamePosition(other.Position, arrived.Position));

        if (standing is null)
        {
            return;
        }

        standing.AddUnits(arrived.Units, arrived.Strength);
        state.RemoveArmy(arrived.Id);
        events.Add(new ArmiesMerged(state.Tick, arrived.Id, standing.Id));
    }
}
