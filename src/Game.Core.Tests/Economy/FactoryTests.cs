using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Economy;

/// <summary>
/// Fabriken der Testdaten: Walzwerk (2 Holz + 0,5 Fisch → 1 Schiene, 2 Stufen) und Schreinerei (3 Holz → 1 Schiene).
/// Takt 4 Ticks. Provinzproduktion läuft vor den Fabriken: a (Rot) +1,5 Holz, b (Blau) +0,25 Fisch, c (Blau) +1,5 Holz.
/// </summary>
public class FactoryTests
{
    private readonly GameData _data = TestData.Load();

    [Fact]
    public void Factory_RunsOncePerLevelPerInterval()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 2);
        SetStock(state, Red, wood: 10, fish: 5);

        Step(state, 4);

        var red = state.GetNation(Red);
        Assert.Equal(2000, red.GetResource(Rails));
        Assert.Equal(10_000 + 1500 - 4000, red.GetResource(Wood));
        Assert.Equal(5000 - 1000, red.GetResource(Fish));
        Assert.Equal(2, red.LastFactoryRuns[Railworks.Value]);
    }

    [Fact]
    public void Factory_DoesNotRunBetweenIntervals()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 1);
        SetStock(state, Red, wood: 10, fish: 5);

        Step(state, 3);

        Assert.Equal(0, state.GetNation(Red).GetResource(Rails));
    }

    [Fact]
    public void Factory_StopsWhenInputIsMissing_WithoutPartialProduction()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 2);
        SetStock(state, Red, wood: 10, fish: 0.75m);

        Step(state, 4);

        var red = state.GetNation(Red);
        Assert.Equal(1000, red.GetResource(Rails));
        Assert.Equal(250, red.GetResource(Fish));
        Assert.Equal(1, red.LastFactoryRuns[Railworks.Value]);
    }

    [Fact]
    public void Capacity_CountsLevelsOfOwnProvincesOnly()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 2);
        state.GetProvince(B).SetBuildingLevel(Railworks, 1);
        state.GetProvince(C).SetBuildingLevel(Railworks, 2);

        Assert.Equal(2, FactoryRules.Capacity(state, Red, Railworks));
        Assert.Equal(3, FactoryRules.Capacity(state, Blue, Railworks));
    }

    [Fact]
    public void Limit_ReducesRunningFactories()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 2);
        SetStock(state, Red, wood: 10, fish: 5);

        Execute(state, Red, new SetFactoryActivityCommand(Railworks, 1));
        Step(state, 3);

        Assert.Equal(1000, state.GetNation(Red).GetResource(Rails));
    }

    [Fact]
    public void Limit_Zero_StopsAllFactoriesOfThatType()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 2);
        SetStock(state, Red, wood: 10, fish: 5);

        Execute(state, Red, new SetFactoryActivityCommand(Railworks, 0));
        Step(state, 3);

        Assert.Equal(0, state.GetNation(Red).GetResource(Rails));
    }

    [Fact]
    public void NoLimit_NewFactoriesRunAutomatically()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 1);
        SetStock(state, Red, wood: 20, fish: 5);
        Step(state, 4);

        state.GetProvince(A).SetBuildingLevel(Railworks, 2);
        Step(state, 4);

        Assert.Equal(1000 + 2000, state.GetNation(Red).GetResource(Rails));
    }

    [Fact]
    public void Limit_AboveCapacityAfterConquest_IsCappedToCapacity()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 2);
        state.GetNation(Red).SetFactoryLimit(Railworks, 2);
        state.GetProvince(A).SetBuildingLevel(Railworks, 1);

        Assert.Equal(1, FactoryRules.ActiveFactories(state, Red, Railworks));
    }

    [Fact]
    public void CompetingFactories_LowerBuildingIdGetsInputsFirst()
    {
        // Walzwerk (ID 1) und Schreinerei (ID 2) brauchen beide Holz; es reicht nur für eines.
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 1);
        state.GetProvince(A).SetBuildingLevel(Carpentry, 1);
        SetStock(state, Red, wood: 2, fish: 5);

        Step(state, 4);

        var red = state.GetNation(Red);
        Assert.Equal(1, red.LastFactoryRuns[Railworks.Value]);
        Assert.Equal(0, red.LastFactoryRuns[Carpentry.Value]);
        Assert.Equal(1500, red.GetResource(Wood));
    }

    [Fact]
    public void FactoryCycle_RunsOnlyEveryNthEconomyInterval()
    {
        var data = DataWithFactoryCycle(4);
        var state = GameStateFactory.CreateNew(data, seed: 1);
        state.GetProvince(A).SetBuildingLevel(Railworks, 1);
        SetStock(state, Red, wood: 50, fish: 50);

        Step(state, data, 15);
        Assert.Equal(0, state.GetNation(Red).GetResource(Rails));

        Step(state, data, 1);
        Assert.Equal(1000, state.GetNation(Red).GetResource(Rails));

        Step(state, data, 16);
        Assert.Equal(2000, state.GetNation(Red).GetResource(Rails));
    }

    [Fact]
    public void FactoryCycle_IncomeIsAveragedPerEconomyInterval()
    {
        var data = DataWithFactoryCycle(4);
        var state = GameStateFactory.CreateNew(data, seed: 1);
        state.GetProvince(A).SetBuildingLevel(Railworks, 1);
        SetStock(state, Red, wood: 50, fish: 50);
        Step(state, data, 16);

        var income = EconomyRules.IncomePerInterval(state, data, Red);

        // 1 Schiene je Zyklus von 4 Takten → 0,25 je Takt; Zutaten 2 Holz und 0,5 Fisch → 0,5 und 0,125 je Takt.
        Assert.Equal([1500 - 500, -125, 250], income.Resources);
    }

    [Fact]
    public void MissingInput_NamesFirstMissingResource()
    {
        var state = NewState();
        SetStock(state, Red, wood: 10, fish: 0);

        Assert.Equal(Fish, FactoryRules.MissingInput(state.GetNation(Red), _data.GetBuilding(Railworks).Recipe!));
    }

    [Fact]
    public void Income_IncludesFactoryBalanceOfLastInterval()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 1);
        SetStock(state, Red, wood: 10, fish: 5);
        Step(state, 4);

        var income = EconomyRules.IncomePerInterval(state, _data, Red);

        Assert.Equal([1500 - 2000, -500, 1000], income.Resources);
    }

    [Theory]
    [InlineData(0, null, true)]
    [InlineData(0, 0, true)]
    [InlineData(0, 2, true)]
    [InlineData(0, 3, false)]
    [InlineData(0, -1, false)]
    public void SetFactoryActivity_AllowsZeroToCapacityOrAll(int province, int? count, bool valid)
    {
        var state = NewState();
        state.GetProvince(new ProvinceId(province)).SetBuildingLevel(Railworks, 2);

        Assert.Equal(valid, new SetFactoryActivityCommand(Railworks, count).Validate(state, _data, Red).IsValid);
    }

    [Fact]
    public void SetFactoryActivity_NonFactory_IsInvalid()
    {
        Assert.Equal("Unbekannter Fabriktyp.", new SetFactoryActivityCommand(Mine, 0).Validate(NewState(), _data, Red).Reason);
    }

    [Fact]
    public void SetFactoryActivity_Null_RestoresUnlimited()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Railworks, 2);
        Execute(state, Red, new SetFactoryActivityCommand(Railworks, 0));

        Execute(state, Red, new SetFactoryActivityCommand(Railworks, null));

        Assert.Null(state.GetNation(Red).GetFactoryLimit(Railworks));
    }

    private GameState NewState() => GameStateFactory.CreateNew(_data, seed: 1);

    private static GameData DataWithFactoryCycle(int intervals)
    {
        var economy = TestData.Economy();
        economy["factoryCycleIntervals"] = intervals;
        return GameDataLoader.Load(TestData.Files(economy: economy));
    }

    private static void Step(GameState state, GameData data, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            Simulation.Step(state, data, []);
        }
    }

    private static void SetStock(GameState state, NationId nation, decimal wood, decimal fish)
    {
        var n = state.GetNation(nation);
        n.AddResource(Wood, (long)(wood * Quantities.ResourceScale) - n.GetResource(Wood));
        n.AddResource(Fish, (long)(fish * Quantities.ResourceScale) - n.GetResource(Fish));
    }

    private void Execute(GameState state, NationId issuer, Command command) =>
        Simulation.Step(state, _data, [new CommandEnvelope(state.Tick, issuer, 0, command)]);

    private void Step(GameState state, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            Simulation.Step(state, _data, []);
        }
    }
}
