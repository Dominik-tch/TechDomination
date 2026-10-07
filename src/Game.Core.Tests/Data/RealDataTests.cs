using Game.Core.Data;
using Game.Core.Map;
using Game.Core.State;

namespace Game.Core.Tests.Data;

/// <summary>Prüft die echten Dateien aus godot/data/, damit fehlerhafte Daten früh auffallen.</summary>
public class RealDataTests
{
    private readonly GameData _data = GameDataLoader.Load(ReadGodotDataFiles());

    [Fact]
    public void Load_Succeeds()
    {
        Assert.True(_data.TicksPerSecond > 0);
        Assert.NotEmpty(_data.Provinces);
        Assert.NotEmpty(_data.Nations);
    }

    [Fact]
    public void EveryLabel_LiesInsideItsOwnProvince()
    {
        foreach (var province in _data.Provinces)
        {
            Assert.True(
                MapGeometry.FindProvinceAt(_data, province.LabelPosition) == province.Id,
                $"Die Beschriftung von '{province.Key}' liegt nicht in der eigenen Provinz.");
        }
    }

    [Fact]
    public void EveryNation_OwnsAtLeastOneProvinceAtStart()
    {
        var state = GameStateFactory.CreateNew(_data, seed: 1);

        foreach (var nation in _data.Nations)
        {
            Assert.True(
                state.Provinces.Any(p => p.Owner == nation.Id),
                $"Nation '{nation.Key}' besitzt zu Beginn keine Provinz.");
        }
    }

    private static Dictionary<string, string> ReadGodotDataFiles() =>
        Directory.GetFiles(RepositoryPaths.GodotDataDirectory, "*.json")
            .ToDictionary(path => Path.GetFileName(path), File.ReadAllText, StringComparer.Ordinal);
}
