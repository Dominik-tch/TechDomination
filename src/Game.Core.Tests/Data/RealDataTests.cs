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
        Assert.NotEmpty(_data.SpeedLevels);
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

    [Fact]
    public void EveryBasicResource_HasMapIcon()
    {
        // Godot lädt res://assets/icons/resources/<id>.svg; ohne Symbol fehlt der Rohstoff auf der Karte.
        string iconDirectory = Path.Combine(RepositoryPaths.GodotDirectory, "assets", "icons", "resources");

        foreach (var resource in _data.Resources.Where(r => r.Tier == ResourceTier.Basic))
        {
            Assert.True(
                File.Exists(Path.Combine(iconDirectory, $"{resource.Key}.svg")),
                $"Symbol für Rohstoff '{resource.Key}' fehlt.");
        }
    }

    private static Dictionary<string, string> ReadGodotDataFiles() =>
        Directory.GetFiles(RepositoryPaths.GodotDataDirectory, "*.json")
            .ToDictionary(path => Path.GetFileName(path), File.ReadAllText, StringComparer.Ordinal);
}
