using Game.Core.Data;

namespace Game.Core.Tests.Data;

/// <summary>Allgemeines Laden: Dateien, JSON-Format, simulation.json, Daten-Hash, echte Daten.</summary>
public class GameDataLoaderTests
{
    [Fact]
    public void Load_ValidData_ReadsSimulationValues()
    {
        var data = GameDataLoader.Load(TestData.Files(simulation: TestData.Simulation(25)));

        Assert.Equal(25, data.TicksPerSecond);
        Assert.False(string.IsNullOrEmpty(data.ContentHash));
    }

    [Theory]
    [InlineData(GameDataLoader.SimulationFileName)]
    [InlineData(GameDataLoader.ResourcesFileName)]
    [InlineData(GameDataLoader.NationsFileName)]
    [InlineData(GameDataLoader.MapFileName)]
    public void Load_MissingFile_ThrowsWithFileName(string fileName)
    {
        var files = TestData.Files();
        files.Remove(fileName);

        var error = Assert.Throws<GameDataException>(() => GameDataLoader.Load(files));

        Assert.Contains(fileName, error.Message);
    }

    [Fact]
    public void Load_InvalidJson_ThrowsWithFileName()
    {
        var files = TestData.Files();
        files[GameDataLoader.MapFileName] = "{ nicht json";

        var error = Assert.Throws<GameDataException>(() => GameDataLoader.Load(files));

        Assert.Contains(GameDataLoader.MapFileName, error.Message);
    }

    [Fact]
    public void Load_NullContent_Throws()
    {
        var files = TestData.Files();
        files[GameDataLoader.SimulationFileName] = "null";

        Assert.Throws<GameDataException>(() => GameDataLoader.Load(files));
    }

    [Fact]
    public void Load_MissingTicksPerSecond_ThrowsWithFieldName()
    {
        var error = Assert.Throws<GameDataException>(
            () => GameDataLoader.Load(TestData.Files(simulation: [])));

        Assert.Contains("ticksPerSecond", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Load_TicksPerSecondOutOfRange_Throws(int ticksPerSecond)
    {
        var error = Assert.Throws<GameDataException>(
            () => GameDataLoader.Load(TestData.Files(simulation: TestData.Simulation(ticksPerSecond))));

        Assert.Contains("ticksPerSecond", error.Message);
    }

    [Fact]
    public void Load_AllowsCommentsAndTrailingCommas()
    {
        var files = TestData.Files();
        files[GameDataLoader.SimulationFileName] = """
            {
              // Kommentar
              "ticksPerSecond": 12,
            }
            """;

        Assert.Equal(12, GameDataLoader.Load(files).TicksPerSecond);
    }

    [Fact]
    public void ContentHash_SameContent_IsEqual()
    {
        Assert.Equal(TestData.Load().ContentHash, TestData.Load().ContentHash);
    }

    [Fact]
    public void ContentHash_DifferentContent_Differs()
    {
        Assert.NotEqual(
            GameDataLoader.Load(TestData.Files(simulation: TestData.Simulation(10))).ContentHash,
            GameDataLoader.Load(TestData.Files(simulation: TestData.Simulation(11))).ContentHash);
    }

    [Fact]
    public void ContentHash_IncludesAdditionalFiles()
    {
        var withExtraFile = TestData.Files();
        withExtraFile["other.json"] = "{}";

        Assert.NotEqual(TestData.Load().ContentHash, GameDataLoader.Load(withExtraFile).ContentHash);
    }

    [Fact]
    public void ContentHash_IgnoresLineEndings()
    {
        var lf = TestData.Files();
        lf[GameDataLoader.SimulationFileName] = "{\n  \"ticksPerSecond\": 10\n}\n";
        var crlf = TestData.Files();
        crlf[GameDataLoader.SimulationFileName] = "{\r\n  \"ticksPerSecond\": 10\r\n}\r\n";

        Assert.Equal(GameDataLoader.Load(lf).ContentHash, GameDataLoader.Load(crlf).ContentHash);
    }

    [Fact]
    public void ContentHash_DoesNotDependOnInsertionOrder()
    {
        var files = TestData.Files();
        var reversed = files.Reverse().ToDictionary(StringComparer.Ordinal);

        Assert.Equal(GameDataLoader.Load(files).ContentHash, GameDataLoader.Load(reversed).ContentHash);
    }
}
