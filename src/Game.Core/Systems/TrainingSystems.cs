using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Military;
using Game.Core.State;

namespace Game.Core.Systems;

/// <summary>
/// Ausbildungsaufträge: In jeder Provinz wird immer nur die erste Einheit der Schlange ausgebildet.
/// Ein Auftrag mit Dauer N, der in Tick t startet, ist am Ende von Tick t + N - 1 fertig.
/// </summary>
internal sealed class TrainingSystem : ISimulationSystem
{
    public void Update(GameState state, GameData data, List<GameEvent> events)
    {
        foreach (var province in state.Provinces)
        {
            if (province.TrainingQueue.Count == 0)
            {
                continue;
            }

            province.TrainingRemainingTicks--;
            if (province.TrainingRemainingTicks > 0)
            {
                continue;
            }

            var type = province.TrainingQueue[0];
            province.RemoveTrainingAt(0);
            ArmySpawning.Spawn(state, data, province.Owner, province.Id, type, 1);
            events.Add(new UnitsTrained(state.Tick, province.Id, type, 1, province.Owner, Automatic: false));

            province.TrainingRemainingTicks = province.TrainingQueue.Count > 0
                ? data.GetUnitType(province.TrainingQueue[0]).TrainingTicks
                : 0;
        }
    }
}

/// <summary>Zum Tageswechsel bekommt jede Provinz automatisch und kostenlos Infanterie (mit Kaserne mehr).</summary>
internal sealed class DailyUnitsSystem : ISimulationSystem
{
    public void Update(GameState state, GameData data, List<GameEvent> events)
    {
        if ((state.Tick + 1) % data.TicksPerDay != 0)
        {
            return;
        }

        foreach (var province in state.Provinces)
        {
            int bonus = data.Buildings.Where(b => province.GetBuildingLevel(b.Id) > 0).Sum(b => b.DailyUnitsBonus);
            foreach (var unitType in data.UnitTypes.Where(u => u.DailyPerProvince > 0))
            {
                int count = unitType.DailyPerProvince + bonus;
                ArmySpawning.Spawn(state, data, province.Owner, province.Id, unitType.Id, count);
                events.Add(new UnitsTrained(state.Tick, province.Id, unitType.Id, count, province.Owner, Automatic: true));
            }
        }
    }
}

/// <summary>
/// Pro Wirtschaftstakt wird der Unterhalt aller Armeen aus dem nationalen Pool bezahlt. Reicht der Vorrat nicht,
/// wird der Rest verbraucht und alle betroffenen Einheiten verlieren anteilig zum Fehlbetrag Stärke. Bei 0 lösen sie sich auf.
/// </summary>
internal sealed class UpkeepSystem : ISimulationSystem
{
    private const long BasisPointsPerWhole = 10_000;

    public void Update(GameState state, GameData data, List<GameEvent> events)
    {
        if (!Economy.EconomyRules.IsEconomyTick(state.Tick, data))
        {
            return;
        }

        foreach (var nation in state.Nations)
        {
            var armies = state.Armies.Where(a => a.Owner == nation.Id).ToList();
            var needed = Economy.EconomyRules.UpkeepPerInterval(state, data, nation.Id);

            // Fehlender Anteil je Ressource in Basispunkten.
            var missing = new long[needed.Length];
            bool shortage = false;
            for (int r = 0; r < needed.Length; r++)
            {
                if (needed[r] == 0)
                {
                    continue;
                }

                var resource = new ResourceId(r);
                long consumed = Math.Min(needed[r], Math.Max(0, nation.GetResource(resource)));
                nation.AddResource(resource, -consumed);
                missing[r] = (needed[r] - consumed) * BasisPointsPerWhole / needed[r];
                shortage |= consumed < needed[r];
            }

            if (!shortage)
            {
                continue;
            }

            events.Add(new UpkeepShortage(state.Tick, nation.Id));
            foreach (var army in armies)
            {
                ApplyStrengthLoss(army, data, missing);
                if (army.TotalUnits == 0)
                {
                    state.RemoveArmy(army.Id);
                }
            }
        }
    }

    private static void ApplyStrengthLoss(ArmyState army, GameData data, long[] missing)
    {
        foreach (var unitType in data.UnitTypes)
        {
            if (army.GetUnits(unitType.Id) == 0)
            {
                continue;
            }

            // Maßgeblich ist die knappste Ressource, die dieser Typ braucht.
            long worst = 0;
            for (int r = 0; r < missing.Length; r++)
            {
                if (unitType.Upkeep[r] > 0)
                {
                    worst = Math.Max(worst, missing[r]);
                }
            }

            if (worst == 0)
            {
                continue;
            }

            long loss = Math.Max(1, data.Economy.ShortageStrengthLoss * worst / BasisPointsPerWhole);
            army.ReduceStrength(unitType.Id, (int)loss);
        }
    }
}
