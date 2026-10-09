using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.Events;
using Game.Core.State;

namespace Game.Core.Systems;

/// <summary>Jede Provinz zahlt pro Wirtschaftstakt einen festen Steuerbetrag an ihren Besitzer.</summary>
internal sealed class TaxSystem : ISimulationSystem
{
    public void Update(GameState state, GameData data, List<GameEvent> events)
    {
        if (!EconomyRules.IsEconomyTick(state.Tick, data))
        {
            return;
        }

        foreach (var province in state.Provinces)
        {
            state.GetNation(province.Owner).Money += data.Economy.TaxPerProvince;
        }
    }
}
