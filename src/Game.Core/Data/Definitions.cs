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
/// <param name="BasePrice">Basispreis pro Einheit auf dem Markt, in Cent.</param>
public sealed record ResourceDefinition(
    ResourceId Id, string Key, string Name, ResourceTier Tier, long ProductionPerInterval, long BasePrice);

/// <summary>Rezept einer Fabrik: ein Durchlauf verbraucht die Zutaten und erzeugt das Produkt.</summary>
/// <param name="Inputs">Zutaten pro Durchlauf in Tausendstel, Index = <see cref="ResourceId.Value"/>.</param>
/// <param name="OutputAmount">Erzeugte Menge pro Durchlauf in Tausendstel.</param>
public sealed record RecipeDefinition(IReadOnlyList<long> Inputs, ResourceId Output, long OutputAmount);

/// <summary>Marktregeln aus market.json. Alle Anteile in Basispunkten (10 000 = 100 %).</summary>
/// <param name="PriceChangePerUnit">Preisänderung je gekaufter/verkaufter Einheit, bezogen auf den Basispreis.</param>
/// <param name="RecoveryPerInterval">Anteil des Abstands zum Basispreis, um den sich der Preis pro Wirtschaftstakt erholt.</param>
/// <param name="MinPrice">Untergrenze des Preises, bezogen auf den Basispreis.</param>
/// <param name="SellPrice">Anteil des aktuellen Preises, den man beim Verkaufen erhält.</param>
public sealed record MarketDefinition(long PriceChangePerUnit, long RecoveryPerInterval, long MinPrice, long SellPrice);

/// <summary>Kosten und Bauzeit einer Gebäudestufe.</summary>
/// <param name="Money">Geld in Cent.</param>
/// <param name="Resources">Ressourcen in Tausendstel, Index = <see cref="ResourceId.Value"/>.</param>
/// <param name="BuildTicks">Bauzeit in Ticks (≥ 1).</param>
public sealed record BuildingLevelDefinition(long Money, IReadOnlyList<long> Resources, int BuildTicks);

/// <summary>Gebäudetyp aus buildings.json.</summary>
/// <param name="Key">Lesbare ID aus der Datendatei.</param>
/// <param name="Levels">Stufe 1 bis Maximalstufe; Index 0 = Stufe 1.</param>
/// <param name="ProductionBonusPerLevel">
/// Erhöhung des Rohstoff-Outputs der Provinz je Stufe in Basispunkten des Basiswerts (1000 = 10 %).
/// </param>
/// <param name="Recipe">Rezept, wenn das Gebäude eine Fabrik ist; jede Stufe zählt wie eine eigene Fabrik.</param>
public sealed record BuildingDefinition(
    BuildingId Id,
    string Key,
    string Name,
    IReadOnlyList<BuildingLevelDefinition> Levels,
    long ProductionBonusPerLevel,
    RecipeDefinition? Recipe)
{
    public int MaxLevel => Levels.Count;

    public bool IsFactory => Recipe is not null;

    /// <summary>Kosten und Bauzeit für die angegebene Stufe (1 bis <see cref="MaxLevel"/>).</summary>
    public BuildingLevelDefinition Level(int level) => Levels[level - 1];
}

/// <summary>Wirtschaftsregeln aus economy.json.</summary>
/// <param name="IntervalTicks">Alle wie viele Ticks Produktion und Steuern gutgeschrieben werden.</param>
/// <param name="TaxPerProvince">Steuern pro Provinz und Takt, in Cent.</param>
/// <param name="StartMoney">Startgeld jeder Nation, in Cent.</param>
/// <param name="StartResources">Startbestand jeder Nation je Ressource in Tausendstel, Index = <see cref="ResourceId.Value"/>.</param>
/// <param name="FactoryCycleIntervals">Fabriken machen nur alle so viele Wirtschaftstakte einen Durchlauf je Stufe.</param>
public sealed record EconomyDefinition(
    int IntervalTicks, long TaxPerProvince, long StartMoney, IReadOnlyList<long> StartResources, int FactoryCycleIntervals);

/// <summary>Nation aus nations.json.</summary>
/// <param name="Key">Lesbare ID aus der Datendatei.</param>
/// <param name="Color">Farbe als <c>#RRGGBB</c>.</param>
public sealed record NationDefinition(NationId Id, string Key, string Name, string Color);

/// <summary>Einheitentyp aus units.json.</summary>
/// <param name="Key">Lesbare ID aus der Datendatei.</param>
/// <param name="Speed">Tempo in Tausendstel Karteneinheiten pro Tick (≥ 1).</param>
/// <param name="Range">Reichweite in Tausendstel Karteneinheiten (0 = keine Fernwirkung). Wirkt ab dem Kampf (M8).</param>
public sealed record UnitTypeDefinition(UnitTypeId Id, string Key, string Name, long Speed, long Range);

/// <summary>Provinz aus map.json: unveränderliche Eigenschaften. Der Besitzer zur Laufzeit steht im Spielzustand.</summary>
/// <param name="Key">Lesbare ID aus der Datendatei.</param>
/// <param name="Resource">Der Basis-Rohstoff, den die Provinz produziert.</param>
/// <param name="StartOwner">Besitzer bei Spielbeginn.</param>
/// <param name="Neighbors">Benachbarte Provinzen über Land, sortiert nach ID. Ihre Städte sind über Pfade verbunden.</param>
/// <param name="Outline">Umriss als Polygon in Kartenkoordinaten.</param>
/// <param name="LabelPosition">Position des Provinznamens auf der Karte.</param>
/// <param name="City">Position der Stadt, Knoten im Wegenetz.</param>
/// <param name="StartArmy">Einheiten je Typ, die zu Spielbeginn in der Stadt stehen (Index = <see cref="UnitTypeId.Value"/>).</param>
public sealed record ProvinceDefinition(
    ProvinceId Id,
    string Key,
    string Name,
    ResourceId Resource,
    NationId StartOwner,
    IReadOnlyList<ProvinceId> Neighbors,
    IReadOnlyList<MapPoint> Outline,
    MapPoint LabelPosition,
    MapPoint City,
    IReadOnlyList<int> StartArmy);
