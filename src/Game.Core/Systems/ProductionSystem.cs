using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.State;

namespace Game.Core.Systems;

/// <summary>Jede Provinz liefert pro Wirtschaftstakt ihren Rohstoff in den Pool ihres Besitzers.</summary>
internal sealed class ProductionSystem : ISimulationSystem
{
    public void Update(GameState state, GameData data)
    {
        if (!EconomyRules.IsEconomyTick(state.Tick, data))
        {
            return;
        }

        foreach (var province in data.Provinces)
        {
            var owner = state.GetNation(state.GetProvince(province.Id).Owner);
            owner.AddResource(province.Resource, EconomyRules.ProductionPerInterval(data, province));
        }
    }
}
