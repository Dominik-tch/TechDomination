using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.Events;
using Game.Core.State;

namespace Game.Core.Systems;

/// <summary>Pro Wirtschaftstakt erholen sich alle Marktpreise ein Stück Richtung Basispreis.</summary>
internal sealed class MarketSystem : ISimulationSystem
{
    public void Update(GameState state, GameData data, List<GameEvent> events)
    {
        if (!EconomyRules.IsEconomyTick(state.Tick, data))
        {
            return;
        }

        foreach (var nation in state.Nations)
        {
            foreach (var resource in data.Resources)
            {
                long price = MarketRules.Recover(
                    nation.GetMarketPrice(resource.Id), resource.BasePrice, data.Market.RecoveryPerInterval);
                nation.SetMarketPrice(resource.Id, price);
            }
        }
    }
}
