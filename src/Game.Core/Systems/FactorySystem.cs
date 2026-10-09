using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.Events;
using Game.Core.State;

namespace Game.Core.Systems;

/// <summary>
/// Alle <c>factoryCycleIntervals</c> Wirtschaftstakte macht jede laufende Fabrik einen Rezept-Durchlauf,
/// sofern der nationale Pool die Zutaten hat.
/// Fehlt etwas, stehen die übrigen Fabriken dieses Typs still (keine Teilproduktion).
/// Reihenfolge: Nationen nach ID, Fabriktypen nach ID – so ist bei knappen Zutaten eindeutig, wer zuerst bekommt.
/// </summary>
internal sealed class FactorySystem : ISimulationSystem
{
    public void Update(GameState state, GameData data, List<GameEvent> events)
    {
        if (!EconomyRules.IsFactoryTick(state.Tick, data))
        {
            return;
        }

        foreach (var nation in state.Nations)
        {
            foreach (var building in data.Buildings)
            {
                if (building.Recipe is not { } recipe)
                {
                    continue;
                }

                int active = FactoryRules.ActiveFactories(state, nation.Id, building.Id);
                int runs = 0;
                while (runs < active && FactoryRules.MissingInput(nation, recipe) is null)
                {
                    for (int i = 0; i < recipe.Inputs.Count; i++)
                    {
                        nation.AddResource(new ResourceId(i), -recipe.Inputs[i]);
                    }

                    nation.AddResource(recipe.Output, recipe.OutputAmount);
                    runs++;
                }

                nation.SetLastFactoryRuns(building.Id, runs);
            }
        }
    }
}
