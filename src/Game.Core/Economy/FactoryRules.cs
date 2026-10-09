using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Economy;

/// <summary>Regeln für Fabriken, gemeinsam genutzt von Simulation, Commands und Anzeige.</summary>
public static class FactoryRules
{
    /// <summary>Anzahl der Fabriken eines Typs, die die Nation besitzt. Jede Stufe zählt wie eine eigene Fabrik.</summary>
    public static int Capacity(GameState state, NationId nation, BuildingId factory)
    {
        ArgumentNullException.ThrowIfNull(state);

        int capacity = 0;
        foreach (var province in state.Provinces)
        {
            if (province.Owner == nation)
            {
                capacity += province.GetBuildingLevel(factory);
            }
        }

        return capacity;
    }

    /// <summary>Wie viele dieser Fabriken laufen sollen: alle, außer der Schieberegler begrenzt sie.</summary>
    public static int ActiveFactories(GameState state, NationId nation, BuildingId factory)
    {
        int capacity = Capacity(state, nation, factory);
        return state.GetNation(nation).GetFactoryLimit(factory) is { } limit ? Math.Min(limit, capacity) : capacity;
    }

    /// <summary>Die erste Zutat, die für einen weiteren Durchlauf fehlt, oder <c>null</c>, wenn alles da ist.</summary>
    public static ResourceId? MissingInput(NationState nation, RecipeDefinition recipe)
    {
        ArgumentNullException.ThrowIfNull(nation);
        ArgumentNullException.ThrowIfNull(recipe);

        for (int i = 0; i < recipe.Inputs.Count; i++)
        {
            if (nation.GetResource(new ResourceId(i)) < recipe.Inputs[i])
            {
                return new ResourceId(i);
            }
        }

        return null;
    }
}
