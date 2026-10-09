using System.Text.Json.Nodes;
using Game.Core.Data;

namespace Game.Core.Tests.Data;

public class SpeedLevelLoadingTests
{
    [Fact]
    public void SpeedLevels_AreLoadedInFileOrder()
    {
        var data = TestData.Load();

        Assert.Equal(["slow", "normal", "fast"], data.SpeedLevels.Select(l => l.Key));
        Assert.Equal([5, 10, 20], data.SpeedLevels.Select(l => l.TicksPerSecond));
        Assert.Equal("Schnell", data.SpeedLevels[2].Name);
    }

    [Fact]
    public void DefaultSpeedLevel_IsResolved()
    {
        Assert.Equal("normal", TestData.Load().DefaultSpeedLevel.Key);
    }

    [Fact]
    public void FindSpeedLevel_ReturnsLevelOrNull()
    {
        var data = TestData.Load();

        Assert.Equal(20, data.FindSpeedLevel("fast")?.TicksPerSecond);
        Assert.Null(data.FindSpeedLevel("ludicrous"));
        Assert.Null(data.FindSpeedLevel("Fast"));
    }

    [Fact]
    public void EmptyList_Throws()
    {
        var simulation = TestData.Simulation();
        simulation["speedLevels"] = new JsonArray();

        var error = LoadFails(simulation);

        Assert.Contains("'speedLevels'", error.Message);
    }

    [Fact]
    public void DuplicateId_Throws()
    {
        var simulation = TestData.Simulation();
        simulation["speedLevels"]![2]!["id"] = "slow";

        var error = LoadFails(simulation);

        Assert.Contains("'slow' ist doppelt", error.Message);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("ticksPerSecond")]
    public void MissingField_Throws(string field)
    {
        var simulation = TestData.Simulation();
        simulation["speedLevels"]![1]!.AsObject().Remove(field);

        var error = LoadFails(simulation);

        Assert.Contains("Geschwindigkeitsstufe 'normal'", error.Message);
        Assert.Contains($"'{field}'", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void TicksPerSecondOutOfRange_Throws(int ticksPerSecond)
    {
        var simulation = TestData.Simulation();
        simulation["speedLevels"]![0]!["ticksPerSecond"] = ticksPerSecond;

        var error = LoadFails(simulation);

        Assert.Contains("'ticksPerSecond'", error.Message);
    }

    [Fact]
    public void MissingDefault_Throws()
    {
        var simulation = TestData.Simulation();
        simulation.Remove("defaultSpeedLevel");

        var error = LoadFails(simulation);

        Assert.Contains("'defaultSpeedLevel'", error.Message);
    }

    [Fact]
    public void UnknownDefault_Throws()
    {
        var simulation = TestData.Simulation();
        simulation["defaultSpeedLevel"] = "turbo";

        var error = LoadFails(simulation);

        Assert.Contains("unbekannte Stufe 'turbo'", error.Message);
    }

    private static GameDataException LoadFails(JsonObject simulation) =>
        Assert.Throws<GameDataException>(() => GameDataLoader.Load(TestData.Files(simulation: simulation)));
}
