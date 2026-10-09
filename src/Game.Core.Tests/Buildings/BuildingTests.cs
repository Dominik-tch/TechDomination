using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.Events;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Buildings;

/// <summary>
/// Gebäude auf der Testkarte. Startbestand je Nation: 100 Geld, 10 Holz, 0,5 Fisch.
/// Mine Stufe 1: 10 Geld, 2 Holz, 3 Ticks. Stufe 2: 20 Geld, 4 Holz, 0,5 Fisch, 5 Ticks. +10 % je Stufe.
/// a (Rot) produziert 1,5 Holz pro Takt (4 Ticks).
/// </summary>
public class BuildingTests
{
    private readonly GameData _data = TestData.Load();

    [Fact]
    public void Build_PaysCostsImmediatelyAndStartsConstruction()
    {
        var state = NewState();

        var events = Execute(state, Red, new BuildBuildingCommand(A, Mine));

        Assert.Empty(events);
        var red = state.GetNation(Red);
        Assert.Equal(100_00 - 10_00, red.Money);
        Assert.Equal(10_000 - 2000, red.GetResource(Wood));
        var construction = Assert.IsType<ConstructionState>(state.GetProvince(A).Construction);
        Assert.Equal(Mine, construction.Building);
        Assert.Equal(1, construction.TargetLevel);
        Assert.Equal(0, state.GetProvince(A).GetBuildingLevel(Mine));
    }

    [Fact]
    public void Construction_CompletesAfterExactlyBuildTicks()
    {
        var state = NewState();
        Execute(state, Red, new BuildBuildingCommand(A, Mine));
        Step(state, 1);
        Assert.Equal(0, state.GetProvince(A).GetBuildingLevel(Mine));

        var events = Step(state, 1);

        Assert.Equal(1, state.GetProvince(A).GetBuildingLevel(Mine));
        Assert.Null(state.GetProvince(A).Construction);
        var completed = Assert.IsType<BuildingCompleted>(Assert.Single(events));
        Assert.Equal(new BuildingCompleted(2, A, Mine, 1, Red), completed);
    }

    [Fact]
    public void Upgrade_BuildsNextLevelWithItsCosts()
    {
        var state = NewState();
        Execute(state, Red, new BuildBuildingCommand(A, Mine));
        Step(state, 2);
        state.GetNation(Red).AddResource(Fish, 1000);

        Execute(state, Red, new BuildBuildingCommand(A, Mine));
        Assert.Equal(2, state.GetProvince(A).Construction!.TargetLevel);
        Step(state, 4);

        Assert.Equal(2, state.GetProvince(A).GetBuildingLevel(Mine));
        Assert.Equal(100_00 - 10_00 - 20_00 + TaxesSoFar(state), state.GetNation(Red).Money);
    }

    [Fact]
    public void Cancel_RefundsFullCostsAndClearsConstruction()
    {
        var state = NewState();
        Execute(state, Red, new BuildBuildingCommand(A, Mine));
        long moneyAfterPaying = state.GetNation(Red).Money;

        var events = Execute(state, Red, new CancelConstructionCommand(A));

        Assert.Empty(events);
        Assert.Null(state.GetProvince(A).Construction);
        Assert.Equal(moneyAfterPaying + 10_00, state.GetNation(Red).Money);
        Assert.Equal(10_000, state.GetNation(Red).GetResource(Wood));
        Step(state, 5);
        Assert.Equal(0, state.GetProvince(A).GetBuildingLevel(Mine));
    }

    [Fact]
    public void Validate_ForeignProvince_IsInvalid()
    {
        Assert.Equal("Die Provinz gehört nicht dir.", Validate(new BuildBuildingCommand(B, Mine), Red));
    }

    [Fact]
    public void Validate_SecondConstructionInSameProvince_IsInvalid()
    {
        var state = NewState();
        Execute(state, Red, new BuildBuildingCommand(A, Mine));

        Assert.Equal("In dieser Provinz wird bereits gebaut.", new BuildBuildingCommand(A, Mine).Validate(state, _data, Red).Reason);
    }

    [Fact]
    public void ConstructionsInDifferentProvinces_RunInParallel()
    {
        var state = NewState();

        var events = Execute(state, Blue, new BuildBuildingCommand(B, Mine), new BuildBuildingCommand(C, Mine));

        Assert.Empty(events);
        Assert.NotNull(state.GetProvince(B).Construction);
        Assert.NotNull(state.GetProvince(C).Construction);
    }

    [Fact]
    public void Validate_MaxLevelReached_IsInvalid()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Mine, 2);

        Assert.Equal("Mine hat bereits die höchste Stufe.", new BuildBuildingCommand(A, Mine).Validate(state, _data, Red).Reason);
    }

    [Fact]
    public void Validate_NotEnoughMoney_NamesAmounts()
    {
        var state = NewState();
        state.GetNation(Red).Money = 5_50;

        Assert.Equal(
            "Nicht genug Geld (benötigt 10, vorhanden 5,5).",
            new BuildBuildingCommand(A, Mine).Validate(state, _data, Red).Reason);
    }

    [Fact]
    public void Validate_NotEnoughResource_NamesResourceAndAmounts()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Mine, 1);

        // Stufe 2 braucht 0,5 Fisch; zuerst fehlt aber nichts – also Fisch wegnehmen.
        state.GetNation(Red).AddResource(Fish, -250);

        Assert.Equal(
            "Nicht genug Fisch (benötigt 0,5, vorhanden 0,25).",
            new BuildBuildingCommand(A, Mine).Validate(state, _data, Red).Reason);
    }

    [Theory]
    [InlineData(99, 0)]
    [InlineData(0, 99)]
    public void Validate_UnknownIds_AreInvalidWithoutException(int province, int building)
    {
        var reason = Validate(new BuildBuildingCommand(new ProvinceId(province), new BuildingId(building)), Red);

        Assert.StartsWith("Unbekannte", reason);
    }

    [Fact]
    public void Cancel_WithoutConstruction_IsInvalid()
    {
        Assert.Equal("In dieser Provinz wird nicht gebaut.", Validate(new CancelConstructionCommand(A), Red));
    }

    [Fact]
    public void Cancel_ForeignConstruction_IsInvalid()
    {
        var state = NewState();
        Execute(state, Blue, new BuildBuildingCommand(B, Mine));

        Assert.Equal("Die Provinz gehört nicht dir.", new CancelConstructionCommand(B).Validate(state, _data, Red).Reason);
    }

    [Fact]
    public void RejectedBuild_ChangesNothing()
    {
        var state = NewState();
        state.GetNation(Red).Money = 0;

        var events = Execute(state, Red, new BuildBuildingCommand(A, Mine));

        Assert.IsType<CommandRejected>(Assert.Single(events));
        Assert.Null(state.GetProvince(A).Construction);
        Assert.Equal(10_000, state.GetNation(Red).GetResource(Wood));
    }

    [Theory]
    [InlineData(0, 1500)]
    [InlineData(1, 1650)]
    [InlineData(2, 1800)]
    public void ProductionBonus_IsBasedOnBaseValue(int level, long expected)
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Mine, level);

        Assert.Equal(expected, EconomyRules.ProductionPerInterval(state, _data, _data.GetProvince(A)));
    }

    [Fact]
    public void ProductionBonus_IsCreditedAndShownInIncome()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Mine, 2);
        long woodBefore = state.GetNation(Red).GetResource(Wood);

        Step(state, 4);

        Assert.Equal(1800, state.GetNation(Red).GetResource(Wood) - woodBefore);
        Assert.Equal(1800, EconomyRules.IncomePerInterval(state, _data, Red).Resources[Wood.Value]);
    }

    private GameState NewState() => GameStateFactory.CreateNew(_data, seed: 1);

    private string? Validate(Command command, NationId issuer) => command.Validate(NewState(), _data, issuer).Reason;

    private IReadOnlyList<GameEvent> Execute(GameState state, NationId issuer, params Command[] commands) =>
        Simulation.Step(state, _data, commands.Select((c, i) => new CommandEnvelope(state.Tick, issuer, i, c)).ToList());

    private List<GameEvent> Step(GameState state, int ticks)
    {
        var events = new List<GameEvent>();
        for (int i = 0; i < ticks; i++)
        {
            events.AddRange(Simulation.Step(state, _data, []));
        }

        return events;
    }

    // Rot besitzt eine Provinz: 2,50 Steuern je vollendetem Takt.
    private long TaxesSoFar(GameState state) => state.Tick / _data.Economy.IntervalTicks * 2_50;
}
