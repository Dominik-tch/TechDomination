using Game.Core.Data;

namespace Game.Core.Tests.Data;

public class GameDataLoaderTests
{
    [Fact]
    public void Load_ValidData_ReadsValues()
    {
        var data = GameDataLoader.Load(TestData.Files(ticksPerSecond: 25));

        Assert.Equal(25, data.TicksPerSecond);
        Assert.False(string.IsNullOrEmpty(data.ContentHash));
    }

    [Fact]
    public void Load_MissingFile_ThrowsWithFileName()
    {
        var files = new Dictionary<string, string>();

        var error = Assert.Throws<GameDataException>(() => GameDataLoader.Load(files));

        Assert.Contains(GameDataLoader.SimulationFileName, error.Message);
    }

    [Fact]
    public void Load_InvalidJson_ThrowsWithFileName()
    {
        var files = new Dictionary<string, string> { [GameDataLoader.SimulationFileName] = "{ nicht json" };

        var error = Assert.Throws<GameDataException>(() => GameDataLoader.Load(files));

        Assert.Contains(GameDataLoader.SimulationFileName, error.Message);
    }

    [Fact]
    public void Load_NullContent_Throws()
    {
        var files = new Dictionary<string, string> { [GameDataLoader.SimulationFileName] = "null" };

        Assert.Throws<GameDataException>(() => GameDataLoader.Load(files));
    }

    [Fact]
    public void Load_MissingField_ThrowsWithFieldName()
    {
        var files = new Dictionary<string, string> { [GameDataLoader.SimulationFileName] = "{}" };

        var error = Assert.Throws<GameDataException>(() => GameDataLoader.Load(files));

        Assert.Contains("ticksPerSecond", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1001)]
    public void Load_TicksPerSecondOutOfRange_Throws(int ticksPerSecond)
    {
        var error = Assert.Throws<GameDataException>(
            () => GameDataLoader.Load(TestData.Files(ticksPerSecond)));

        Assert.Contains("ticksPerSecond", error.Message);
    }

    [Fact]
    public void Load_AllowsCommentsAndTrailingCommas()
    {
        var files = new Dictionary<string, string>
        {
            [GameDataLoader.SimulationFileName] = """
                {
                  // Kommentar
                  "ticksPerSecond": 12,
                }
                """,
        };

        Assert.Equal(12, GameDataLoader.Load(files).TicksPerSecond);
    }

    [Fact]
    public void ContentHash_SameContent_IsEqual()
    {
        Assert.Equal(
            GameDataLoader.Load(TestData.Files(10)).ContentHash,
            GameDataLoader.Load(TestData.Files(10)).ContentHash);
    }

    [Fact]
    public void ContentHash_DifferentContent_Differs()
    {
        Assert.NotEqual(
            GameDataLoader.Load(TestData.Files(10)).ContentHash,
            GameDataLoader.Load(TestData.Files(11)).ContentHash);
    }

    [Fact]
    public void ContentHash_IncludesAdditionalFiles()
    {
        var withExtraFile = TestData.Files();
        withExtraFile["other.json"] = "{}";

        Assert.NotEqual(
            GameDataLoader.Load(TestData.Files()).ContentHash,
            GameDataLoader.Load(withExtraFile).ContentHash);
    }

    [Fact]
    public void ContentHash_IgnoresLineEndings()
    {
        var lf = new Dictionary<string, string> { [GameDataLoader.SimulationFileName] = "{\n  \"ticksPerSecond\": 10\n}\n" };
        var crlf = new Dictionary<string, string> { [GameDataLoader.SimulationFileName] = "{\r\n  \"ticksPerSecond\": 10\r\n}\r\n" };

        Assert.Equal(GameDataLoader.Load(lf).ContentHash, GameDataLoader.Load(crlf).ContentHash);
    }

    [Fact]
    public void ContentHash_DoesNotDependOnInsertionOrder()
    {
        var first = new Dictionary<string, string>
        {
            [GameDataLoader.SimulationFileName] = TestData.Files()[GameDataLoader.SimulationFileName],
            ["a.json"] = "{}",
        };
        var second = new Dictionary<string, string>
        {
            ["a.json"] = "{}",
            [GameDataLoader.SimulationFileName] = TestData.Files()[GameDataLoader.SimulationFileName],
        };

        Assert.Equal(GameDataLoader.Load(first).ContentHash, GameDataLoader.Load(second).ContentHash);
    }

    [Fact]
    public void Load_RealGodotDataFiles_Succeeds()
    {
        var files = Directory.GetFiles(RepositoryPaths.GodotDataDirectory, "*.json")
            .ToDictionary(path => Path.GetFileName(path), File.ReadAllText, StringComparer.Ordinal);

        var data = GameDataLoader.Load(files);

        Assert.True(data.TicksPerSecond > 0);
    }
}
