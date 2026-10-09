namespace Game.Core.Data;

// Abbild der JSON-Dateien in godot/data/. Alle Felder sind nullable, damit fehlende Felder
// beim Validieren mit einer klaren Meldung erkannt werden statt still einen Standardwert zu bekommen.

internal sealed class SimulationFile
{
    public int? TicksPerDay { get; set; }

    public List<SpeedLevelEntry?>? SpeedLevels { get; set; }

    public string? DefaultSpeedLevel { get; set; }
}

internal sealed class SpeedLevelEntry
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public int? TicksPerSecond { get; set; }
}

internal sealed class ResourcesFile
{
    public List<ResourceEntry?>? Resources { get; set; }
}

internal sealed class ResourceEntry
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Tier { get; set; }

    public decimal? Production { get; set; }

    public decimal? BasePrice { get; set; }
}

internal sealed class MarketFile
{
    public decimal? PriceChangePercentPerUnit { get; set; }

    public decimal? RecoveryPercentPerInterval { get; set; }

    public decimal? MinPricePercent { get; set; }

    public decimal? SellPricePercent { get; set; }
}

internal sealed class BuildingsFile
{
    public List<BuildingEntry?>? Buildings { get; set; }
}

internal sealed class BuildingEntry
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public decimal? ProductionBonusPercent { get; set; }

    public RecipeEntry? Recipe { get; set; }

    public int? DailyUnitsBonus { get; set; }

    public List<BuildingLevelEntry?>? Levels { get; set; }
}

internal sealed class RecipeEntry
{
    public Dictionary<string, decimal>? Inputs { get; set; }

    public string? Output { get; set; }

    public decimal? Amount { get; set; }
}

internal sealed class BuildingLevelEntry
{
    public decimal? Money { get; set; }

    public Dictionary<string, decimal>? Resources { get; set; }

    public int? BuildTicks { get; set; }
}

internal sealed class EconomyFile
{
    public int? IntervalTicks { get; set; }

    public decimal? TaxPerProvince { get; set; }

    public decimal? StartMoney { get; set; }

    public Dictionary<string, decimal>? StartResources { get; set; }

    public int? FactoryCycleIntervals { get; set; }

    public decimal? ShortageStrengthLossPercent { get; set; }
}

internal sealed class NationsFile
{
    public List<NationEntry?>? Nations { get; set; }
}

internal sealed class NationEntry
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Color { get; set; }
}

internal sealed class MapFile
{
    public List<ProvinceEntry?>? Provinces { get; set; }
}

internal sealed class ProvinceEntry
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? Resource { get; set; }

    public string? Owner { get; set; }

    public List<string?>? Neighbors { get; set; }

    public List<int[]?>? Outline { get; set; }

    public int[]? Label { get; set; }

    public int[]? City { get; set; }

    public Dictionary<string, int>? StartArmy { get; set; }
}

internal sealed class UnitsFile
{
    public List<UnitEntry?>? Units { get; set; }
}

internal sealed class UnitEntry
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public decimal? Speed { get; set; }

    public decimal? Range { get; set; }

    public int? DailyPerProvince { get; set; }

    public int? TrainingTicks { get; set; }

    public CostEntry? Cost { get; set; }

    public string? Requires { get; set; }

    public Dictionary<string, decimal>? Upkeep { get; set; }
}

internal sealed class CostEntry
{
    public decimal? Money { get; set; }

    public Dictionary<string, decimal>? Resources { get; set; }
}
