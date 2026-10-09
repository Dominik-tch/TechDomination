namespace Game.Core.Data;

// Abbild der JSON-Dateien in godot/data/. Alle Felder sind nullable, damit fehlende Felder
// beim Validieren mit einer klaren Meldung erkannt werden statt still einen Standardwert zu bekommen.

internal sealed class SimulationFile
{
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
}

internal sealed class EconomyFile
{
    public int? IntervalTicks { get; set; }

    public decimal? TaxPerProvince { get; set; }

    public decimal? StartMoney { get; set; }

    public Dictionary<string, decimal>? StartResources { get; set; }
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

    public int? Size { get; set; }

    public string? Owner { get; set; }

    public List<string?>? Neighbors { get; set; }

    public List<int[]?>? Outline { get; set; }

    public int[]? Label { get; set; }
}
