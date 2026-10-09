using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Game.Core.Data;

/// <summary>
/// Lädt und validiert das Regelwerk. Kennt keine Pfade: Der Aufrufer übergibt den Inhalt
/// der Dateien (Dateiname relativ zu godot/data/ → Inhalt), siehe docs/decisions/0003.
/// </summary>
/// <remarks>
/// In den Dateien sind IDs lesbare Strings. Daraus werden typisierte Ganzzahl-IDs in der
/// Reihenfolge der Datei vergeben.
/// </remarks>
public static class GameDataLoader
{
    public const string SimulationFileName = "simulation.json";
    public const string ResourcesFileName = "resources.json";
    public const string NationsFileName = "nations.json";
    public const string MapFileName = "map.json";
    public const string EconomyFileName = "economy.json";
    public const string BuildingsFileName = "buildings.json";
    public const string MarketFileName = "market.json";
    public const string UnitsFileName = "units.json";

    // Prozentangaben mit bis zu 2 Nachkommastellen → Basispunkte (10 % = 1000).
    private const long BasisPointsPerPercent = 100;

    // Technische Obergrenze, kein Balancing-Wert.
    private const int MaxTicksPerSecond = 1000;

    private static readonly Regex ColorPattern = new("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant);

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static GameData Load(IReadOnlyDictionary<string, string> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var simulation = Parse<SimulationFile>(files, SimulationFileName);
        var (speedLevels, defaultSpeedLevel) = LoadSpeedLevels(simulation);
        int ticksPerDay = simulation.TicksPerDay ?? throw Error(SimulationFileName, "Feld 'ticksPerDay' fehlt.");
        if (ticksPerDay < 1)
        {
            throw Error(SimulationFileName, $"'ticksPerDay' muss mindestens 1 sein, ist aber {ticksPerDay}.");
        }

        var resources = LoadResources(Parse<ResourcesFile>(files, ResourcesFileName));
        var nations = LoadNations(Parse<NationsFile>(files, NationsFileName));
        var buildings = LoadBuildings(Parse<BuildingsFile>(files, BuildingsFileName), resources);
        var unitTypes = LoadUnitTypes(Parse<UnitsFile>(files, UnitsFileName), resources, buildings);
        var provinces = LoadProvinces(Parse<MapFile>(files, MapFileName), resources, nations, unitTypes);
        var economy = LoadEconomy(Parse<EconomyFile>(files, EconomyFileName), resources);
        var market = LoadMarket(Parse<MarketFile>(files, MarketFileName));

        return new GameData(
            ticksPerDay, speedLevels, defaultSpeedLevel, economy, market, resources, nations, provinces, buildings, unitTypes,
            ComputeContentHash(files));
    }

    /// <summary>
    /// Hash über alle Dateien, sortiert nach Namen. Zeilenenden werden vereinheitlicht,
    /// damit Git-Einstellungen (CRLF/LF) den Hash nicht verändern.
    /// </summary>
    internal static string ComputeContentHash(IReadOnlyDictionary<string, string> files)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (string fileName in files.Keys.Order(StringComparer.Ordinal))
        {
            string content = files[fileName].Replace("\r\n", "\n", StringComparison.Ordinal);
            hash.AppendData(Encoding.UTF8.GetBytes(fileName));
            hash.AppendData([0]);
            hash.AppendData(Encoding.UTF8.GetBytes(content));
            hash.AppendData([0]);
        }

        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static (List<SpeedLevelDefinition> Levels, SpeedLevelDefinition Default) LoadSpeedLevels(SimulationFile file)
    {
        var entries = RequireEntries(file.SpeedLevels, SimulationFileName, "speedLevels");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var levels = new List<SpeedLevelDefinition>(entries.Count);

        foreach (var entry in entries)
        {
            string key = RequireKey(entry.Id, keys, SimulationFileName, "Geschwindigkeitsstufe");
            string context = $"Geschwindigkeitsstufe '{key}'";
            string name = RequireText(entry.Name, SimulationFileName, context, "name");
            int ticksPerSecond = entry.TicksPerSecond
                ?? throw Error(SimulationFileName, $"{context}: Feld 'ticksPerSecond' fehlt.");

            if (ticksPerSecond is < 1 or > MaxTicksPerSecond)
            {
                throw Error(
                    SimulationFileName,
                    $"{context}: 'ticksPerSecond' muss zwischen 1 und {MaxTicksPerSecond} liegen, ist aber {ticksPerSecond}.");
            }

            levels.Add(new SpeedLevelDefinition(key, name, ticksPerSecond));
        }

        string defaultKey = RequireText(file.DefaultSpeedLevel, SimulationFileName, "Datei", "defaultSpeedLevel");
        var defaultLevel = levels.FirstOrDefault(level => level.Key == defaultKey)
            ?? throw Error(SimulationFileName, $"'defaultSpeedLevel' verweist auf unbekannte Stufe '{defaultKey}'.");

        return (levels, defaultLevel);
    }

    private static List<ResourceDefinition> LoadResources(ResourcesFile file)
    {
        var entries = RequireEntries(file.Resources, ResourcesFileName, "resources");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var resources = new List<ResourceDefinition>(entries.Count);

        foreach (var entry in entries)
        {
            string key = RequireKey(entry.Id, keys, ResourcesFileName, "Ressource");
            string name = RequireText(entry.Name, ResourcesFileName, $"Ressource '{key}'", "name");
            var tier = entry.Tier switch
            {
                "basic" => ResourceTier.Basic,
                "advanced" => ResourceTier.Advanced,
                _ => throw Error(
                    ResourcesFileName,
                    $"Ressource '{key}': 'tier' muss \"basic\" oder \"advanced\" sein, ist aber \"{entry.Tier}\"."),
            };

            long production = tier switch
            {
                ResourceTier.Basic => RequireAmount(
                    entry.Production, Quantities.ResourceScale, ResourcesFileName, $"Ressource '{key}'", "production"),
                _ when entry.Production is not null => throw Error(
                    ResourcesFileName,
                    $"Ressource '{key}': Fortgeschrittene Güter werden nicht von Provinzen produziert und haben kein 'production'."),
                _ => 0,
            };

            long basePrice = RequireAmount(entry.BasePrice, Quantities.MoneyScale, ResourcesFileName, $"Ressource '{key}'", "basePrice");
            if (basePrice == 0)
            {
                throw Error(ResourcesFileName, $"Ressource '{key}': 'basePrice' muss größer als 0 sein.");
            }

            resources.Add(new ResourceDefinition(new ResourceId(resources.Count), key, name, tier, production, basePrice));
        }

        return resources;
    }

    private static EconomyDefinition LoadEconomy(EconomyFile file, IReadOnlyList<ResourceDefinition> resources)
    {
        const string context = "Datei";

        int intervalTicks = file.IntervalTicks ?? throw Error(EconomyFileName, "Feld 'intervalTicks' fehlt.");
        if (intervalTicks < 1)
        {
            throw Error(EconomyFileName, $"'intervalTicks' muss mindestens 1 sein, ist aber {intervalTicks}.");
        }

        long taxPerProvince = RequireAmount(file.TaxPerProvince, Quantities.MoneyScale, EconomyFileName, context, "taxPerProvince");
        long startMoney = RequireAmount(file.StartMoney, Quantities.MoneyScale, EconomyFileName, context, "startMoney");

        var startResources = LoadResourceAmounts(file.StartResources, resources, EconomyFileName, "'startResources'");

        int factoryCycle = file.FactoryCycleIntervals ?? throw Error(EconomyFileName, "Feld 'factoryCycleIntervals' fehlt.");
        if (factoryCycle < 1)
        {
            throw Error(EconomyFileName, $"'factoryCycleIntervals' muss mindestens 1 sein, ist aber {factoryCycle}.");
        }

        long shortageLoss = file.ShortageStrengthLossPercent is null
            ? 0
            : RequireAmount(file.ShortageStrengthLossPercent, BasisPointsPerPercent, EconomyFileName, context, "shortageStrengthLossPercent");

        return new EconomyDefinition(intervalTicks, taxPerProvince, startMoney, startResources, factoryCycle, shortageLoss);
    }

    private static List<BuildingDefinition> LoadBuildings(BuildingsFile file, IReadOnlyList<ResourceDefinition> resources)
    {
        var entries = RequireEntries(file.Buildings, BuildingsFileName, "buildings");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var buildings = new List<BuildingDefinition>(entries.Count);

        foreach (var entry in entries)
        {
            string key = RequireKey(entry.Id, keys, BuildingsFileName, "Gebäude");
            string context = $"Gebäude '{key}'";
            string name = RequireText(entry.Name, BuildingsFileName, context, "name");

            // Ohne Angabe: keine Wirkung auf die Produktion.
            long bonus = entry.ProductionBonusPercent is null
                ? 0
                : RequireAmount(entry.ProductionBonusPercent, BasisPointsPerPercent, BuildingsFileName, context, "productionBonusPercent");
            var recipe = entry.Recipe is null ? null : LoadRecipe(entry.Recipe, resources, context);

            var levelEntries = RequireEntries(entry.Levels, BuildingsFileName, "levels");
            var levels = new List<BuildingLevelDefinition>(levelEntries.Count);
            foreach (var level in levelEntries)
            {
                string levelContext = $"{context}, Stufe {levels.Count + 1}";
                long money = RequireAmount(level.Money, Quantities.MoneyScale, BuildingsFileName, levelContext, "money");
                var costs = LoadResourceAmounts(level.Resources, resources, BuildingsFileName, $"{levelContext}, 'resources'");
                int buildTicks = level.BuildTicks
                    ?? throw Error(BuildingsFileName, $"{levelContext}: Feld 'buildTicks' fehlt.");
                if (buildTicks < 1)
                {
                    throw Error(BuildingsFileName, $"{levelContext}: 'buildTicks' muss mindestens 1 sein, ist aber {buildTicks}.");
                }

                levels.Add(new BuildingLevelDefinition(money, costs, buildTicks));
            }

            int dailyUnitsBonus = entry.DailyUnitsBonus ?? 0;
            if (dailyUnitsBonus < 0)
            {
                throw Error(BuildingsFileName, $"{context}: 'dailyUnitsBonus' darf nicht negativ sein.");
            }

            buildings.Add(new BuildingDefinition(new BuildingId(buildings.Count), key, name, levels, bonus, recipe, dailyUnitsBonus));
        }

        return buildings;
    }

    private static RecipeDefinition LoadRecipe(RecipeEntry entry, IReadOnlyList<ResourceDefinition> resources, string context)
    {
        string recipeContext = $"{context}, Rezept";
        var inputs = LoadResourceAmounts(entry.Inputs, resources, BuildingsFileName, $"{recipeContext}, 'inputs'");
        if (inputs.All(amount => amount == 0))
        {
            throw Error(BuildingsFileName, $"{recipeContext}: 'inputs' braucht mindestens eine Zutat.");
        }

        string outputKey = RequireText(entry.Output, BuildingsFileName, recipeContext, "output");
        var output = resources.FirstOrDefault(r => r.Key == outputKey)
            ?? throw Error(BuildingsFileName, $"{recipeContext}: unbekannte Ressource '{outputKey}'.");
        if (output.Tier != ResourceTier.Advanced)
        {
            throw Error(BuildingsFileName, $"{recipeContext}: '{outputKey}' ist kein fortgeschrittenes Gut.");
        }

        long amount = RequireAmount(entry.Amount, Quantities.ResourceScale, BuildingsFileName, recipeContext, "amount");
        if (amount == 0)
        {
            throw Error(BuildingsFileName, $"{recipeContext}: 'amount' muss größer als 0 sein.");
        }

        return new RecipeDefinition(inputs, output.Id, amount);
    }

    private static MarketDefinition LoadMarket(MarketFile file)
    {
        const string context = "Datei";
        return new MarketDefinition(
            RequirePercent(file.PriceChangePercentPerUnit, context, "priceChangePercentPerUnit"),
            RequirePercent(file.RecoveryPercentPerInterval, context, "recoveryPercentPerInterval"),
            RequirePercent(file.MinPricePercent, context, "minPricePercent"),
            RequirePercent(file.SellPricePercent, context, "sellPricePercent"));
    }

    /// <summary>Prozentwert zwischen 0 und 100 aus market.json, in Basispunkten.</summary>
    private static long RequirePercent(decimal? value, string context, string field)
    {
        long basisPoints = RequireAmount(value, BasisPointsPerPercent, MarketFileName, context, field);
        if (basisPoints > 100 * BasisPointsPerPercent)
        {
            throw Error(MarketFileName, $"'{field}' darf höchstens 100 sein, ist aber {value}.");
        }

        return basisPoints;
    }

    /// <summary>Ressourcenmengen nach lesbarer ID. Fehlende Ressourcen sind 0; unbekannte sind ein Tippfehler.</summary>
    private static long[] LoadResourceAmounts(
        Dictionary<string, decimal>? amounts, IReadOnlyList<ResourceDefinition> resources, string fileName, string context)
    {
        var result = new long[resources.Count];

        // Sortiert, damit bei mehreren Fehlern immer derselbe gemeldet wird.
        foreach (var (resourceKey, amount) in (amounts ?? []).OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var resource = resources.FirstOrDefault(r => r.Key == resourceKey)
                ?? throw Error(fileName, $"{context} enthält unbekannte Ressource '{resourceKey}'.");
            result[resource.Id.Value] = RequireAmount(amount, Quantities.ResourceScale, fileName, context, resourceKey);
        }

        return result;
    }

    /// <summary>Pflichtfeld mit nicht-negativer Dezimalzahl, exakt umgerechnet in die Untereinheit.</summary>
    private static long RequireAmount(decimal? value, long scale, string fileName, string context, string field)
    {
        decimal amount = value ?? throw Error(fileName, $"{context}: Feld '{field}' fehlt.");
        if (amount < 0)
        {
            throw Error(fileName, $"{context}: '{field}' darf nicht negativ sein, ist aber {amount}.");
        }

        return Quantities.ToFixed(amount, scale)
            ?? throw Error(
                fileName,
                $"{context}: '{field}' = {amount} hat zu viele Nachkommastellen (höchstens {scale.ToString(CultureInfo.InvariantCulture).Length - 1}).");
    }

    private static List<NationDefinition> LoadNations(NationsFile file)
    {
        var entries = RequireEntries(file.Nations, NationsFileName, "nations");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var nations = new List<NationDefinition>(entries.Count);

        foreach (var entry in entries)
        {
            string key = RequireKey(entry.Id, keys, NationsFileName, "Nation");
            string name = RequireText(entry.Name, NationsFileName, $"Nation '{key}'", "name");
            string color = RequireText(entry.Color, NationsFileName, $"Nation '{key}'", "color");
            if (!ColorPattern.IsMatch(color))
            {
                throw Error(NationsFileName, $"Nation '{key}': 'color' muss das Format #RRGGBB haben, ist aber \"{color}\".");
            }

            nations.Add(new NationDefinition(new NationId(nations.Count), key, name, color));
        }

        return nations;
    }

    private static List<UnitTypeDefinition> LoadUnitTypes(
        UnitsFile file, IReadOnlyList<ResourceDefinition> resources, IReadOnlyList<BuildingDefinition> buildings)
    {
        var entries = RequireEntries(file.Units, UnitsFileName, "units");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var unitTypes = new List<UnitTypeDefinition>(entries.Count);

        foreach (var entry in entries)
        {
            string key = RequireKey(entry.Id, keys, UnitsFileName, "Einheit");
            string context = $"Einheit '{key}'";
            string name = RequireText(entry.Name, UnitsFileName, context, "name");
            long speed = RequireAmount(entry.Speed, Map.FinePoint.Scale, UnitsFileName, context, "speed");
            if (speed == 0)
            {
                throw Error(UnitsFileName, $"{context}: 'speed' muss größer als 0 sein.");
            }

            long range = entry.Range is null ? 0 : RequireAmount(entry.Range, Map.FinePoint.Scale, UnitsFileName, context, "range");

            int daily = entry.DailyPerProvince ?? 0;
            int trainingTicks = entry.TrainingTicks ?? 0;
            if (daily < 0 || trainingTicks < 0)
            {
                throw Error(UnitsFileName, $"{context}: 'dailyPerProvince' und 'trainingTicks' dürfen nicht negativ sein.");
            }

            if (daily == 0 && trainingTicks == 0)
            {
                throw Error(UnitsFileName, $"{context}: braucht 'dailyPerProvince' (automatisch) oder 'trainingTicks' (per Auftrag).");
            }

            long costMoney = entry.Cost?.Money is null
                ? 0
                : RequireAmount(entry.Cost.Money, Quantities.MoneyScale, UnitsFileName, context, "cost.money");
            var costResources = LoadResourceAmounts(entry.Cost?.Resources, resources, UnitsFileName, $"{context}, 'cost.resources'");
            var upkeep = LoadResourceAmounts(entry.Upkeep, resources, UnitsFileName, $"{context}, 'upkeep'");

            BuildingId? required = null;
            if (entry.Requires is { } requiredKey)
            {
                required = buildings.FirstOrDefault(b => b.Key == requiredKey)?.Id
                    ?? throw Error(UnitsFileName, $"{context}: unbekanntes Gebäude '{requiredKey}' in 'requires'.");
            }

            unitTypes.Add(new UnitTypeDefinition(
                new UnitTypeId(unitTypes.Count), key, name, speed, range,
                daily, trainingTicks, costMoney, costResources, required, upkeep));
        }

        return unitTypes;
    }

    private static List<ProvinceDefinition> LoadProvinces(
        MapFile file,
        IReadOnlyList<ResourceDefinition> resources,
        IReadOnlyList<NationDefinition> nations,
        IReadOnlyList<UnitTypeDefinition> unitTypes)
    {
        var entries = RequireEntries(file.Provinces, MapFileName, "provinces");

        // Erster Durchlauf: IDs vergeben, damit Nachbarn in beliebiger Reihenfolge referenziert werden können.
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var idsByKey = new Dictionary<string, ProvinceId>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            string key = RequireKey(entry.Id, keys, MapFileName, "Provinz");
            idsByKey[key] = new ProvinceId(idsByKey.Count);
        }

        var resourcesByKey = resources.ToDictionary(r => r.Key, StringComparer.Ordinal);
        var nationsByKey = nations.ToDictionary(n => n.Key, StringComparer.Ordinal);
        var provinces = new List<ProvinceDefinition>(entries.Count);

        foreach (var entry in entries)
        {
            string key = entry.Id!;
            string context = $"Provinz '{key}'";

            string name = RequireText(entry.Name, MapFileName, context, "name");

            string resourceKey = RequireText(entry.Resource, MapFileName, context, "resource");
            if (!resourcesByKey.TryGetValue(resourceKey, out var resource))
            {
                throw Error(MapFileName, $"{context}: unbekannte Ressource '{resourceKey}'.");
            }

            if (resource.Tier != ResourceTier.Basic)
            {
                throw Error(MapFileName, $"{context}: '{resourceKey}' ist kein Basis-Rohstoff.");
            }

            string ownerKey = RequireText(entry.Owner, MapFileName, context, "owner");
            if (!nationsByKey.TryGetValue(ownerKey, out var owner))
            {
                throw Error(MapFileName, $"{context}: unbekannte Nation '{ownerKey}'.");
            }

            var neighbors = LoadNeighbors(entry, key, context, idsByKey);
            var outline = LoadOutline(entry, context);
            var label = ToPoint(entry.Label, context, "label");
            var city = ToPoint(entry.City, context, "city");
            if (!Map.MapGeometry.Contains(outline, city))
            {
                throw Error(MapFileName, $"{context}: Die Stadt liegt nicht in der Provinz.");
            }

            var startArmy = LoadStartArmy(entry.StartArmy, unitTypes, context);

            provinces.Add(new ProvinceDefinition(
                idsByKey[key], key, name, resource.Id, owner.Id, neighbors, outline, label, city, startArmy));
        }

        RequireMutualNeighbors(provinces);
        return provinces;
    }

    private static List<ProvinceId> LoadNeighbors(
        ProvinceEntry entry, string key, string context, IReadOnlyDictionary<string, ProvinceId> idsByKey)
    {
        // Eine Provinz ohne Nachbarn (Insel) ist erlaubt: Landeinheiten erreichen sie dann nicht.
        var neighbors = new List<ProvinceId>();
        foreach (string? neighborKey in entry.Neighbors ?? [])
        {
            if (string.IsNullOrWhiteSpace(neighborKey) || !idsByKey.TryGetValue(neighborKey, out var neighbor))
            {
                throw Error(MapFileName, $"{context}: unbekannter Nachbar '{neighborKey}'.");
            }

            if (neighborKey == key)
            {
                throw Error(MapFileName, $"{context}: Eine Provinz kann nicht ihr eigener Nachbar sein.");
            }

            if (neighbors.Contains(neighbor))
            {
                throw Error(MapFileName, $"{context}: Nachbar '{neighborKey}' ist doppelt eingetragen.");
            }

            neighbors.Add(neighbor);
        }

        neighbors.Sort((a, b) => a.Value.CompareTo(b.Value));
        return neighbors;
    }

    private static List<MapPoint> LoadOutline(ProvinceEntry entry, string context)
    {
        if (entry.Outline is not { Count: >= 3 })
        {
            throw Error(MapFileName, $"{context}: 'outline' braucht mindestens 3 Punkte.");
        }

        return entry.Outline.Select(point => ToPoint(point, context, "outline")).ToList();
    }

    private static int[] LoadStartArmy(
        Dictionary<string, int>? entries, IReadOnlyList<UnitTypeDefinition> unitTypes, string context)
    {
        var counts = new int[unitTypes.Count];
        foreach (var (unitKey, count) in (entries ?? []).OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            var unitType = unitTypes.FirstOrDefault(u => u.Key == unitKey)
                ?? throw Error(MapFileName, $"{context}: 'startArmy' enthält unbekannte Einheit '{unitKey}'.");
            if (count < 0)
            {
                throw Error(MapFileName, $"{context}: 'startArmy' – Anzahl von '{unitKey}' darf nicht negativ sein.");
            }

            counts[unitType.Id.Value] = count;
        }

        return counts;
    }

    private static void RequireMutualNeighbors(IReadOnlyList<ProvinceDefinition> provinces)
    {
        foreach (var province in provinces)
        {
            foreach (var neighborId in province.Neighbors)
            {
                var neighbor = provinces[neighborId.Value];
                if (!neighbor.Neighbors.Contains(province.Id))
                {
                    throw Error(
                        MapFileName,
                        $"Nachbarschaft ist nicht gegenseitig: '{province.Key}' nennt '{neighbor.Key}', aber nicht umgekehrt.");
                }
            }
        }
    }

    private static MapPoint ToPoint(int[]? values, string context, string field)
    {
        if (values is not { Length: 2 })
        {
            throw Error(MapFileName, $"{context}: '{field}' enthält einen Punkt, der nicht aus genau zwei Zahlen [x, y] besteht.");
        }

        return new MapPoint(values[0], values[1]);
    }

    private static List<T> RequireEntries<T>(List<T?>? entries, string fileName, string field)
        where T : class
    {
        if (entries is not { Count: > 0 })
        {
            throw Error(fileName, $"Liste '{field}' fehlt oder ist leer.");
        }

        if (entries.Any(e => e is null))
        {
            throw Error(fileName, $"Liste '{field}' enthält einen leeren Eintrag (null).");
        }

        return entries!;
    }

    private static string RequireKey(string? key, HashSet<string> knownKeys, string fileName, string kind)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw Error(fileName, $"{kind} ohne 'id'.");
        }

        if (!knownKeys.Add(key))
        {
            throw Error(fileName, $"{kind} '{key}' ist doppelt vorhanden.");
        }

        return key;
    }

    private static string RequireText(string? value, string fileName, string context, string field) =>
        string.IsNullOrWhiteSpace(value) ? throw Error(fileName, $"{context}: Feld '{field}' fehlt.") : value;

    private static GameDataException Error(string fileName, string message) => new($"{fileName}: {message}");

    private static T Parse<T>(IReadOnlyDictionary<string, string> files, string fileName)
        where T : class
    {
        if (!files.TryGetValue(fileName, out string? content))
        {
            throw new GameDataException($"Datendatei '{fileName}' fehlt.");
        }

        T? result;
        try
        {
            result = JsonSerializer.Deserialize<T>(content, Options);
        }
        catch (JsonException e)
        {
            throw new GameDataException($"{fileName}: ungültiges JSON – {e.Message}", e);
        }

        return result ?? throw Error(fileName, "Datei ist leer (null).");
    }
}
