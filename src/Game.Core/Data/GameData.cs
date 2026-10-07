namespace Game.Core.Data;

/// <summary>Unveränderliches Regelwerk, geladen aus den Dateien in godot/data/.</summary>
public sealed class GameData
{
    internal GameData(
        int ticksPerSecond,
        IReadOnlyList<ResourceDefinition> resources,
        IReadOnlyList<NationDefinition> nations,
        IReadOnlyList<ProvinceDefinition> provinces,
        string contentHash)
    {
        TicksPerSecond = ticksPerSecond;
        Resources = resources;
        Nations = nations;
        Provinces = provinces;
        ContentHash = contentHash;
    }

    /// <summary>
    /// Ticks pro Sekunde bei Normalgeschwindigkeit. Nur die Darstellungsschicht nutzt den Wert,
    /// um Echtzeit in Ticks umzurechnen; die Simulation selbst kennt keine Sekunden.
    /// </summary>
    public int TicksPerSecond { get; }

    /// <summary>Alle Ressourcen, Index = <see cref="ResourceId.Value"/>.</summary>
    public IReadOnlyList<ResourceDefinition> Resources { get; }

    /// <summary>Alle Nationen, Index = <see cref="NationId.Value"/>.</summary>
    public IReadOnlyList<NationDefinition> Nations { get; }

    /// <summary>Alle Provinzen, Index = <see cref="ProvinceId.Value"/>.</summary>
    public IReadOnlyList<ProvinceDefinition> Provinces { get; }

    /// <summary>SHA-256 über alle Datendateien. Erkennt abweichende Regelwerke bei Spielständen und Netzwerk-Beitritt.</summary>
    public string ContentHash { get; }

    public ResourceDefinition GetResource(ResourceId id) => Resources[id.Value];

    public NationDefinition GetNation(NationId id) => Nations[id.Value];

    public ProvinceDefinition GetProvince(ProvinceId id) => Provinces[id.Value];
}
