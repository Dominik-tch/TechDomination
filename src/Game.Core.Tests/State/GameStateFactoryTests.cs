using Game.Core.State;

namespace Game.Core.Tests.State;

public class GameStateFactoryTests
{
    [Fact]
    public void CreateNew_StartsAtTickZeroWithSeed()
    {
        var state = GameStateFactory.CreateNew(TestData.Load(), seed: 1);

        Assert.Equal(0, state.Tick);
        Assert.Equal(1UL, state.Seed);
    }

    [Fact]
    public void CreateNew_HasOneStatePerProvinceInIdOrder()
    {
        var data = TestData.Load();

        var state = GameStateFactory.CreateNew(data, seed: 1);

        Assert.Equal(data.Provinces.Select(p => p.Id), state.Provinces.Select(p => p.Id));
    }

    [Fact]
    public void CreateNew_UsesStartOwnersFromMap()
    {
        var data = TestData.Load();

        var state = GameStateFactory.CreateNew(data, seed: 1);

        Assert.Equal(["red", "blue", "blue"], state.Provinces.Select(p => data.GetNation(p.Owner).Key));
    }

    [Fact]
    public void CreateNew_ProvincesStartWithoutBuildingsOrConstruction()
    {
        var state = GameStateFactory.CreateNew(TestData.Load(), seed: 1);

        Assert.All(state.Provinces, province =>
        {
            Assert.Equal([0, 0, 0], province.BuildingLevels);
            Assert.Null(province.Construction);
        });
    }

    [Fact]
    public void CreateNew_PlacesStartArmiesInCities()
    {
        var state = GameStateFactory.CreateNew(TestData.Load(), seed: 1);

        Assert.Equal(2, state.Armies.Count);
        var red = state.FindArmy(TestIds.RedArmy)!;
        Assert.Equal(TestIds.Red, red.Owner);
        Assert.Equal(Game.Core.Map.PathPosition.AtCity(TestIds.A), red.Position);
        Assert.Equal([2, 0, 0], red.Units);
        Assert.False(red.IsMoving);
        Assert.Equal(TestIds.Blue, state.FindArmy(TestIds.BlueArmy)!.Owner);
        Assert.Equal(2, state.NextArmyId);
    }

    [Fact]
    public void Constructor_RejectsArmyIdsNotBelowNextId()
    {
        var army = new ArmyState(new ArmyId(3), TestIds.Red, Game.Core.Map.PathPosition.AtCity(TestIds.A), [1, 0, 0], []);

        Assert.Throws<ArgumentException>(
            () => new GameState(1, 0, Game.Core.Determinism.DeterministicRandom.FromSeed(1), [], [], [army], 3));
    }

    [Fact]
    public void FindArmy_UnknownId_IsNull()
    {
        Assert.Null(GameStateFactory.CreateNew(TestData.Load(), seed: 1).FindArmy(new ArmyId(42)));
    }

    [Fact]
    public void GetProvince_ReturnsStateById()
    {
        var state = GameStateFactory.CreateNew(TestData.Load(), seed: 1);

        Assert.Equal(new ProvinceId(2), state.GetProvince(new ProvinceId(2)).Id);
    }

    [Fact]
    public void Constructor_RejectsUnsortedProvinces()
    {
        var provinces = new List<ProvinceState>
        {
            new(new ProvinceId(1), new NationId(0), [], null),
            new(new ProvinceId(0), new NationId(0), [], null),
        };

        Assert.Throws<ArgumentException>(
            () => new GameState(1, 0, Game.Core.Determinism.DeterministicRandom.FromSeed(1), [], provinces, [], 0));
    }

    [Fact]
    public void Constructor_RejectsUnsortedNations()
    {
        var nations = new List<NationState> { new(new NationId(1), 0, [], [], [], []), new(new NationId(0), 0, [], [], [], []) };

        Assert.Throws<ArgumentException>(
            () => new GameState(1, 0, Game.Core.Determinism.DeterministicRandom.FromSeed(1), nations, [], [], 0));
    }

    [Fact]
    public void CreateNew_GivesEveryNationStartMoneyAndResources()
    {
        var data = TestData.Load();

        var state = GameStateFactory.CreateNew(data, seed: 1);

        Assert.Equal(data.Nations.Select(n => n.Id), state.Nations.Select(n => n.Id));
        Assert.All(state.Nations, nation =>
        {
            Assert.Equal(100_00, nation.Money);
            Assert.Equal([10_000, 500, 0], nation.Resources);
        });
    }

    [Fact]
    public void CreateNew_MarketStartsAtBasePricesAndFactoriesUnlimited()
    {
        var state = GameStateFactory.CreateNew(TestData.Load(), seed: 1);

        Assert.All(state.Nations, nation =>
        {
            Assert.Equal([10_00, 4_00, 50_00], nation.MarketPrices);
            Assert.Equal([null, null, null], nation.FactoryLimits);
            Assert.Equal([0, 0, 0], nation.LastFactoryRuns);
        });
    }

    [Fact]
    public void CreateNew_NationsDoNotShareResourceArrays()
    {
        var state = GameStateFactory.CreateNew(TestData.Load(), seed: 1);

        state.GetNation(TestIds.Red).AddResource(new ResourceId(0), 1);

        Assert.Equal(10_000, state.GetNation(TestIds.Blue).GetResource(new ResourceId(0)));
    }
}
