namespace Game.Core.Data;

/// <summary>Stufe einer Ressource: Basis-Rohstoff (von Provinzen produziert) oder fortgeschrittenes Gut (aus Fabriken).</summary>
public enum ResourceTier
{
    Basic,
    Advanced,
}

/// <summary>Punkt in Kartenkoordinaten. Ganzzahlig, damit Berechnungen auf der Karte deterministisch bleiben.</summary>
public readonly record struct MapPoint(int X, int Y);

/// <summary>Geschwindigkeitsstufe aus simulation.json.</summary>
/// <param name="Key">Lesbare ID aus der Datendatei.</param>
/// <param name="TicksPerSecond">Ticks pro Sekunde Echtzeit. Nur die Darstellungsschicht rechnet damit; die Simulation kennt keine Sekunden.</param>
public sealed record SpeedLevelDefinition(string Key, string Name, int TicksPerSecond);

/// <summary>Ressource aus resources.json.</summary>
/// <param name="Key">Lesbare ID aus der Datendatei.</param>
/// <param name="ProductionPerInterval">
/// Menge, die eine Provinz mit diesem Rohstoff pro Wirtschaftstakt produziert, in Tausendstel.
/// 0 bei fortgeschrittenen Gütern, die nur in Fabriken entstehen.
/// </param>
public sealed record ResourceDefinition(
    ResourceId Id, string Key, string Name, ResourceTier Tier, long ProductionPerInterval);

/// <summary>Wirtschaftsregeln aus economy.json.</summary>
/// <param name="IntervalTicks">Alle wie viele Ticks Produktion und Steuern gutgeschrieben werden.</param>
/// <param name="TaxPerProvince">Steuern pro Provinz und Takt, in Cent.</param>
/// <param name="StartMoney">Startgeld jeder Nation, in Cent.</param>
/// <param name="StartResources">Startbestand jeder Nation je Ressource in Tausendstel, Index = <see cref="ResourceId.Value"/>.</param>
public sealed record EconomyDefinition(
    int IntervalTicks, long TaxPerProvince, long StartMoney, IReadOnlyList<long> StartResources);

/// <summary>Nation aus nations.json.</summary>
/// <param name="Key">Lesbare ID aus der Datendatei.</param>
/// <param name="Color">Farbe als <c>#RRGGBB</c>.</param>
public sealed record NationDefinition(NationId Id, string Key, string Name, string Color);

/// <summary>Provinz aus map.json: unveränderliche Eigenschaften. Der Besitzer zur Laufzeit steht im Spielzustand.</summary>
/// <param name="Key">Lesbare ID aus der Datendatei.</param>
/// <param name="Resource">Der Basis-Rohstoff, den die Provinz produziert.</param>
/// <param name="Size">Größe der Provinz (≥ 1). Bestimmt später die Bewegungsdauer.</param>
/// <param name="StartOwner">Besitzer bei Spielbeginn.</param>
/// <param name="Neighbors">Benachbarte Provinzen über Land, sortiert nach ID.</param>
/// <param name="Outline">Umriss als Polygon in Kartenkoordinaten.</param>
/// <param name="LabelPosition">Position des Provinznamens auf der Karte.</param>
public sealed record ProvinceDefinition(
    ProvinceId Id,
    string Key,
    string Name,
    ResourceId Resource,
    int Size,
    NationId StartOwner,
    IReadOnlyList<ProvinceId> Neighbors,
    IReadOnlyList<MapPoint> Outline,
    MapPoint LabelPosition);
