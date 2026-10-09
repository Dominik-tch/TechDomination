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
        Assert.NotEmpty(_data.Buildings);
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
    public void EveryPath_RunsOnlyThroughItsTwoProvinces()
    {
        // Stichproben entlang jedes Pfads: Jeder Punkt muss in einer der beiden verbundenen Provinzen liegen,
        // sonst führt der Weg sichtbar durch eine dritte Provinz.
        foreach (var (a, b) in _data.Graph.Edges)
        {
            long length = _data.Graph.Length(a, b);
            for (int i = 0; i <= 20; i++)
            {
                var point = _data.Graph.PointOf(new Game.Core.Map.PathPosition(a, b, length * i / 20)).ToMap();
                bool inA = MapGeometry.Contains(_data.GetProvince(a).Outline, point);
                bool inB = MapGeometry.Contains(_data.GetProvince(b).Outline, point);
                Assert.True(inA || inB, $"Pfad {_data.GetProvince(a).Key} – {_data.GetProvince(b).Key} verlässt bei {point} die beiden Provinzen.");
            }
        }
    }

    [Fact]
    public void Infantry_NeedsFourToFiveMinutesPerProvinceOnAverageAtNormalSpeed()
    {
        // Anforderung: Infanterie braucht bei Normalgeschwindigkeit im Schnitt 4–5 Minuten pro Provinz.
        var infantry = _data.UnitTypes.Single(u => u.Key == "infantry");
        var lengths = _data.Graph.Edges.Select(e => _data.Graph.Length(e.A, e.B)).ToList();
        long averageTicks = (long)lengths.Average() / infantry.Speed;
        long ticksPerMinute = _data.FindSpeedLevel("normal")!.TicksPerSecond * 60L;

        Assert.InRange(averageTicks, 4 * ticksPerMinute, 5 * ticksPerMinute);
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
    public void EveryAdvancedResource_IsProducedByAFactory()
    {
        foreach (var resource in _data.Resources.Where(r => r.Tier == ResourceTier.Advanced))
        {
            Assert.True(
                _data.Buildings.Any(b => b.Recipe?.Output == resource.Id),
                $"Kein Rezept erzeugt '{resource.Key}'.");
        }
    }

    [Fact]
    public void Factories_HaveFiveLevels()
    {
        Assert.All(_data.Buildings.Where(b => b.IsFactory), factory => Assert.Equal(5, factory.MaxLevel));
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
