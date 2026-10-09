using System.Text.Json.Nodes;
using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.Events;
using Game.Core.Map;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Military;

/// <summary>
/// Unterhalt mit eigenen Testdaten: Infanterie braucht 0,1 Fisch pro Takt, Kavallerie 0,2 Fisch und 0,5 Holz.
/// Takt 4 Ticks. Vollständiger Mangel kostet 10 % Stärke pro Takt. Rot: 2 Infanterie in a; Blau: 1 Kavallerie in c.
/// Provinzproduktion pro Takt: a (Rot) 1,5 Holz, b (Blau) 0,25 Fisch, c (Blau) 1,5 Holz – sie läuft vor dem Unterhalt.
/// </summary>
public class UpkeepTests
{
    private readonly GameData _data = DataWithUpkeep();

    [Fact]
    public void Upkeep_IsPaidFromPoolEachInterval()
    {
        var state = NewState();
        long fishBefore = state.GetNation(Red).GetResource(Fish);

        var events = Step(state, 4);

        Assert.Equal(fishBefore - 200, state.GetNation(Red).GetResource(Fish));
        Assert.Equal(ArmyState.FullStrength, state.FindArmy(RedArmy)!.GetStrength(Infantry));
        Assert.DoesNotContain(events, e => e is UpkeepShortage);
    }

    [Fact]
    public void Income_IncludesUpkeep()
    {
        var income = EconomyRules.IncomePerInterval(NewState(), _data, Red);

        Assert.Equal([1500, -200, 0], income.Resources);
    }

    [Fact]
    public void FullShortage_CostsFullStrengthLossAndReportsEvent()
    {
        var state = NewState();
        SetFish(state, Red, 0);

        var events = Step(state, 4);

        Assert.Equal(9_000, state.FindArmy(RedArmy)!.GetStrength(Infantry));
        Assert.Contains(new UpkeepShortage(3, Red), events);
        Assert.Equal(0, state.GetNation(Red).GetResource(Fish));
    }

    [Fact]
    public void PartialShortage_CostsProportionalStrength()
    {
        // Gebraucht 0,2 Fisch, vorhanden 0,05 → 75 % fehlen → 7,5 % Stärkeverlust.
        var state = NewState();
        SetFish(state, Red, 50);

        Step(state, 4);

        Assert.Equal(9_250, state.FindArmy(RedArmy)!.GetStrength(Infantry));
        Assert.Equal(0, state.GetNation(Red).GetResource(Fish));
    }

    [Fact]
    public void Shortage_OnlyAffectsUnitsNeedingThatResource()
    {
        // Blau hat genug Fisch, aber kein Holz: nur die Kavallerie (braucht Holz) verliert Stärke.
        var state = NewState();
        state.CreateArmy(Blue, PathPosition.AtCity(B), [1, 0, 0]);
        state.GetNation(Blue).AddResource(Wood, -state.GetNation(Blue).GetResource(Wood));
        state.GetProvince(C).Owner = Red;

        Step(state, 4);

        Assert.Equal(9_000, state.FindArmy(BlueArmy)!.GetStrength(Cavalry));
        Assert.Equal(ArmyState.FullStrength, state.Armies.Single(a => a.Owner == Blue && a.Id != BlueArmy).GetStrength(Infantry));
    }

    [Fact]
    public void ZeroStrength_DisbandsUnitsAndRemovesEmptyArmy()
    {
        var state = NewState();
        SetFish(state, Red, 0);

        Step(state, 40);

        Assert.Null(state.FindArmy(RedArmy));
    }

    [Fact]
    public void Merging_AveragesStrengthByCount()
    {
        var state = NewState();
        var weak = state.CreateArmy(Red, PathPosition.AtCity(B), [2, 0, 0], [5_000, ArmyState.FullStrength, ArmyState.FullStrength]);

        weak.AddUnits([2, 0, 0], [ArmyState.FullStrength, ArmyState.FullStrength, ArmyState.FullStrength]);

        Assert.Equal(7_500, weak.GetStrength(Infantry));
        Assert.Equal(4, weak.GetUnits(Infantry));
    }

    [Fact]
    public void Split_KeepsStrengthInBothArmies()
    {
        var state = NewState();
        var army = state.FindArmy(RedArmy)!;
        army.ReduceStrength(Infantry, 2_000);

        Simulation.Step(state, _data, [new CommandEnvelope(0, Red, 0, new SplitArmyCommand(RedArmy, [1, 0, 0], PathPosition.AtCity(B)))]);

        Assert.Equal(8_000, army.GetStrength(Infantry));
        Assert.Equal(8_000, state.Armies[^1].GetStrength(Infantry));
    }

    private GameState NewState() => GameStateFactory.CreateNew(_data, seed: 1);

    private static void SetFish(GameState state, NationId nation, long thousandths)
    {
        var n = state.GetNation(nation);
        n.AddResource(Fish, thousandths - n.GetResource(Fish));
    }

    private List<GameEvent> Step(GameState state, int ticks)
    {
        var events = new List<GameEvent>();
        for (int i = 0; i < ticks; i++)
        {
            events.AddRange(Simulation.Step(state, _data, []));
        }

        return events;
    }

    private static GameData DataWithUpkeep()
    {
        var units = TestData.Units();
        units["units"]![0]!["upkeep"] = JsonNode.Parse("""{ "fish": 0.1 }""");
        units["units"]![1]!["upkeep"] = JsonNode.Parse("""{ "fish": 0.2, "wood": 0.5 }""");
        var economy = TestData.Economy();
        economy["shortageStrengthLossPercent"] = 10;
        return GameDataLoader.Load(TestData.Files(units: units, economy: economy));
    }
}
