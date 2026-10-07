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
            new(new ProvinceId(1), new NationId(0)),
            new(new ProvinceId(0), new NationId(0)),
        };

        Assert.Throws<ArgumentException>(
            () => new GameState(1, 0, Game.Core.Determinism.DeterministicRandom.FromSeed(1), provinces));
    }
}
