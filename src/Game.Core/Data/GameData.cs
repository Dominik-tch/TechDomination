namespace Game.Core.Data;

/// <summary>Unveränderliches Regelwerk, geladen aus den Dateien in godot/data/.</summary>
public sealed class GameData
{
    internal GameData(
        IReadOnlyList<SpeedLevelDefinition> speedLevels,
        SpeedLevelDefinition defaultSpeedLevel,
        EconomyDefinition economy,
        MarketDefinition market,
        IReadOnlyList<ResourceDefinition> resources,
        IReadOnlyList<NationDefinition> nations,
        IReadOnlyList<ProvinceDefinition> provinces,
        IReadOnlyList<BuildingDefinition> buildings,
        string contentHash)
    {
        SpeedLevels = speedLevels;
        DefaultSpeedLevel = defaultSpeedLevel;
        Economy = economy;
        Market = market;
        Resources = resources;
        Nations = nations;
        Provinces = provinces;
        Buildings = buildings;
        ContentHash = contentHash;
    }

    /// <summary>Geschwindigkeitsstufen in der Reihenfolge der Datei.</summary>
    public IReadOnlyList<SpeedLevelDefinition> SpeedLevels { get; }

    /// <summary>Stufe bei Spielbeginn.</summary>
    public SpeedLevelDefinition DefaultSpeedLevel { get; }

    public EconomyDefinition Economy { get; }

    public MarketDefinition Market { get; }

    /// <summary>Alle Ressourcen, Index = <see cref="ResourceId.Value"/>.</summary>
    public IReadOnlyList<ResourceDefinition> Resources { get; }

    /// <summary>Alle Nationen, Index = <see cref="NationId.Value"/>.</summary>
    public IReadOnlyList<NationDefinition> Nations { get; }

    /// <summary>Alle Provinzen, Index = <see cref="ProvinceId.Value"/>.</summary>
    public IReadOnlyList<ProvinceDefinition> Provinces { get; }

    /// <summary>Alle Gebäudetypen, Index = <see cref="BuildingId.Value"/>.</summary>
    public IReadOnlyList<BuildingDefinition> Buildings { get; }

    /// <summary>SHA-256 über alle Datendateien. Erkennt abweichende Regelwerke bei Spielständen und Netzwerk-Beitritt.</summary>
    public string ContentHash { get; }

    /// <summary>Stufe mit der lesbaren ID, oder <c>null</c>, wenn es sie nicht gibt.</summary>
    public SpeedLevelDefinition? FindSpeedLevel(string key) =>
        SpeedLevels.FirstOrDefault(level => string.Equals(level.Key, key, StringComparison.Ordinal));

    public ResourceDefinition GetResource(ResourceId id) => Resources[id.Value];

    public NationDefinition GetNation(NationId id) => Nations[id.Value];

    public ProvinceDefinition GetProvince(ProvinceId id) => Provinces[id.Value];

    public BuildingDefinition GetBuilding(BuildingId id) => Buildings[id.Value];

    public bool Contains(ProvinceId id) => id.Value >= 0 && id.Value < Provinces.Count;

    public bool Contains(NationId id) => id.Value >= 0 && id.Value < Nations.Count;

    public bool Contains(BuildingId id) => id.Value >= 0 && id.Value < Buildings.Count;

    public bool Contains(ResourceId id) => id.Value >= 0 && id.Value < Resources.Count;
}
