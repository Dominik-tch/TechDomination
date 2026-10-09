using Game.Core.Data;

namespace Game.Core.Tests.Data;

/// <summary>Allgemeines Laden: Dateien, JSON-Format, Daten-Hash.</summary>
public class GameDataLoaderTests
{
    [Fact]
    public void Load_ValidData_HasContentHash()
    {
        Assert.False(string.IsNullOrEmpty(TestData.Load().ContentHash));
    }

    [Theory]
    [InlineData(GameDataLoader.SimulationFileName)]
    [InlineData(GameDataLoader.ResourcesFileName)]
    [InlineData(GameDataLoader.NationsFileName)]
    [InlineData(GameDataLoader.MapFileName)]
    [InlineData(GameDataLoader.EconomyFileName)]
    [InlineData(GameDataLoader.BuildingsFileName)]
    [InlineData(GameDataLoader.MarketFileName)]
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
        Assert.NotNull(error.InnerException);
    }

    [Fact]
    public void Load_NullContent_Throws()
    {
        var files = TestData.Files();
        files[GameDataLoader.SimulationFileName] = "null";

        Assert.Throws<GameDataException>(() => GameDataLoader.Load(files));
    }

    [Fact]
    public void Load_AllowsCommentsAndTrailingCommas()
    {
        var files = TestData.Files();
        files[GameDataLoader.SimulationFileName] = """
            {
              // Kommentar
              "speedLevels": [
                { "id": "normal", "name": "Normal", "ticksPerSecond": 12, },
              ],
              "defaultSpeedLevel": "normal",
            }
            """;

        Assert.Equal(12, GameDataLoader.Load(files).DefaultSpeedLevel.TicksPerSecond);
    }

    [Fact]
    public void ContentHash_SameContent_IsEqual()
    {
        Assert.Equal(TestData.Load().ContentHash, TestData.Load().ContentHash);
    }

    [Fact]
    public void ContentHash_DifferentContent_Differs()
    {
        var changed = TestData.Simulation();
        changed["speedLevels"]![0]!["ticksPerSecond"] = 6;

        Assert.NotEqual(TestData.Load().ContentHash, GameDataLoader.Load(TestData.Files(simulation: changed)).ContentHash);
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
        string json = TestData.Simulation().ToJsonString(new() { WriteIndented = true });
        var lf = TestData.Files();
        lf[GameDataLoader.SimulationFileName] = json.ReplaceLineEndings("\n");
        var crlf = TestData.Files();
        crlf[GameDataLoader.SimulationFileName] = json.ReplaceLineEndings("\r\n");

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
