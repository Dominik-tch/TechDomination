using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Economy;

/// <summary>
/// Produktion und Steuern auf der Testkarte: Takt 4 Ticks, Steuern 2,50 pro Provinz,
/// a (Rot) und c (Blau) produzieren 1,5 Holz, b (Blau) 0,25 Fisch.
/// </summary>
public class EconomyTests
{
    private static readonly ResourceId Wood = new(0);
    private static readonly ResourceId Fish = new(1);

    private readonly GameData _data = TestData.Load();

    [Theory]
    [InlineData(0, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(7, true)]
    public void IsEconomyTick_AtEndOfEachInterval(long tick, bool expected)
    {
        Assert.Equal(expected, EconomyRules.IsEconomyTick(tick, _data));
    }

    [Fact]
    public void BeforeFirstInterval_NothingIsCredited()
    {
        var state = Run(ticks: 3);

        Assert.Equal(100_00, state.GetNation(Red).Money);
        Assert.Equal(10_000, state.GetNation(Red).GetResource(Wood));
    }

    [Fact]
    public void AfterOneInterval_ProductionAndTaxesAreCredited()
    {
        var state = Run(ticks: 4);

        var red = state.GetNation(Red);
        Assert.Equal(100_00 + 250, red.Money);
        Assert.Equal(10_000 + 1500, red.GetResource(Wood));
        Assert.Equal(500, red.GetResource(Fish));

        var blue = state.GetNation(Blue);
        Assert.Equal(100_00 + 2 * 250, blue.Money);
        Assert.Equal(10_000 + 1500, blue.GetResource(Wood));
        Assert.Equal(500 + 250, blue.GetResource(Fish));
    }

    [Fact]
    public void AfterSeveralIntervals_CreditsAccumulate()
    {
        var state = Run(ticks: 13);

        Assert.Equal(100_00 + 3 * 2 * 250, state.GetNation(Blue).Money);
        Assert.Equal(500 + 3 * 250, state.GetNation(Blue).GetResource(Fish));
    }

    [Fact]
    public void AdvancedResources_AreNotProduced()
    {
        var state = Run(ticks: 40);

        Assert.All(state.Nations, nation => Assert.Equal(0, nation.GetResource(new ResourceId(2))));
    }

    [Fact]
    public void AfterOwnerChange_NewOwnerReceivesProductionAndTaxes()
    {
        var state = GameStateFactory.CreateNew(_data, seed: 1);
        state.GetProvince(B).Owner = Red;

        Step(state, 4);

        Assert.Equal(100_00 + 2 * 250, state.GetNation(Red).Money);
        Assert.Equal(500 + 250, state.GetNation(Red).GetResource(Fish));
        Assert.Equal(100_00 + 250, state.GetNation(Blue).Money);
        Assert.Equal(500, state.GetNation(Blue).GetResource(Fish));
    }

    [Fact]
    public void NationWithoutProvinces_ReceivesNothing()
    {
        var state = GameStateFactory.CreateNew(_data, seed: 1);
        foreach (var province in state.Provinces)
        {
            province.Owner = Blue;
        }

        Step(state, 8);

        Assert.Equal(100_00, state.GetNation(Red).Money);
        Assert.Equal(EconomyRules.IncomePerInterval(state, _data, Red), new NationIncome(0, [0, 0, 0]), new IncomeComparer());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void IncomePerInterval_MatchesWhatOneIntervalCredits(int nation)
    {
        var id = new NationId(nation);
        var state = GameStateFactory.CreateNew(_data, seed: 1);
        var income = EconomyRules.IncomePerInterval(state, _data, id);
        long moneyBefore = state.GetNation(id).Money;
        var resourcesBefore = state.GetNation(id).Resources.ToArray();

        Step(state, 4);

        Assert.Equal(income.Money, state.GetNation(id).Money - moneyBefore);
        Assert.Equal(income.Resources, state.GetNation(id).Resources.Zip(resourcesBefore, (after, before) => after - before));
    }

    private GameState Run(int ticks)
    {
        var state = GameStateFactory.CreateNew(_data, seed: 1);
        Step(state, ticks);
        return state;
    }

    private void Step(GameState state, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            Simulation.Step(state, _data, []);
        }
    }

    private sealed class IncomeComparer : IEqualityComparer<NationIncome>
    {
        public bool Equals(NationIncome? x, NationIncome? y) =>
            x is not null && y is not null && x.Money == y.Money && x.Resources.SequenceEqual(y.Resources);

        public int GetHashCode(NationIncome obj) => obj.Money.GetHashCode();
    }
}
