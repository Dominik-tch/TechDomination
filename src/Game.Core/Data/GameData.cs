namespace Game.Core.Data;

/// <summary>Unveränderliches Regelwerk, geladen aus den Dateien in godot/data/.</summary>
public sealed class GameData
{
    internal GameData(
        IReadOnlyList<SpeedLevelDefinition> speedLevels,
        SpeedLevelDefinition defaultSpeedLevel,
        IReadOnlyList<ResourceDefinition> resources,
        IReadOnlyList<NationDefinition> nations,
        IReadOnlyList<ProvinceDefinition> provinces,
        string contentHash)
    {
        SpeedLevels = speedLevels;
        DefaultSpeedLevel = defaultSpeedLevel;
        Resources = resources;
        Nations = nations;
        Provinces = provinces;
        ContentHash = contentHash;
    }

    /// <summary>Geschwindigkeitsstufen in der Reihenfolge der Datei.</summary>
    public IReadOnlyList<SpeedLevelDefinition> SpeedLevels { get; }

    /// <summary>Stufe bei Spielbeginn.</summary>
    public SpeedLevelDefinition DefaultSpeedLevel { get; }

    /// <summary>Alle Ressourcen, Index = <see cref="ResourceId.Value"/>.</summary>
    public IReadOnlyList<ResourceDefinition> Resources { get; }

    /// <summary>Alle Nationen, Index = <see cref="NationId.Value"/>.</summary>
    public IReadOnlyList<NationDefinition> Nations { get; }

    /// <summary>Alle Provinzen, Index = <see cref="ProvinceId.Value"/>.</summary>
    public IReadOnlyList<ProvinceDefinition> Provinces { get; }

    /// <summary>SHA-256 über alle Datendateien. Erkennt abweichende Regelwerke bei Spielständen und Netzwerk-Beitritt.</summary>
    public string ContentHash { get; }

    /// <summary>Stufe mit der lesbaren ID, oder <c>null</c>, wenn es sie nicht gibt.</summary>
    public SpeedLevelDefinition? FindSpeedLevel(string key) =>
        SpeedLevels.FirstOrDefault(level => string.Equals(level.Key, key, StringComparison.Ordinal));

    public ResourceDefinition GetResource(ResourceId id) => Resources[id.Value];

    public NationDefinition GetNation(NationId id) => Nations[id.Value];

    public ProvinceDefinition GetProvince(ProvinceId id) => Provinces[id.Value];
}
