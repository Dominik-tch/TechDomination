using System.Text.Json.Nodes;
using Game.Core.Data;

namespace Game.Core.Tests;

/// <summary>
/// Kleine Test-Datensätze, unabhängig von den echten Dateien in godot/data/.
/// Die Builder liefern veränderbare JSON-Objekte, damit Tests gezielt einzelne Felder ändern können.
/// </summary>
/// <remarks>
/// Testkarte: drei Provinzen in einer Reihe, a | b | c, je 10×10 Einheiten.
/// a gehört "red", b und c gehören "blue". a und c produzieren Holz (1,5/Takt), b Fisch (0,25/Takt).
/// Wirtschaftstakt: 4 Ticks, Steuern 2,50 pro Provinz und Takt.
/// Gebäude "mine": +10 % je Stufe, 2 Stufen. Stufe 1: 10 Geld, 2 Holz, 3 Ticks. Stufe 2: 20 Geld, 4 Holz, 0,5 Fisch, 5 Ticks.
/// Fabrik "railworks": 2 Holz + 0,5 Fisch → 1 Schiene, 2 Stufen. Fabrik "carpentry": 3 Holz → 1 Schiene, 1 Stufe.
/// Basispreise: Holz 10, Fisch 4, Schienen 50. Markt: ±10 % des Basispreises je Einheit, Erholung 50 % je Takt,
/// Untergrenze 20 %, Verkauf zu 80 %.
/// </remarks>
internal static class TestData
{
    public static JsonObject Simulation() => Parse("""
        {
          "speedLevels": [
            { "id": "slow", "name": "Langsam", "ticksPerSecond": 5 },
            { "id": "normal", "name": "Normal", "ticksPerSecond": 10 },
            { "id": "fast", "name": "Schnell", "ticksPerSecond": 20 }
          ],
          "defaultSpeedLevel": "normal"
        }
        """);

    public static JsonObject Resources() => Parse("""
        {
          "resources": [
            { "id": "wood", "name": "Holz", "tier": "basic", "production": 1.5, "basePrice": 10 },
            { "id": "fish", "name": "Fisch", "tier": "basic", "production": 0.25, "basePrice": 4 },
            { "id": "rails", "name": "Schienen", "tier": "advanced", "basePrice": 50 }
          ]
        }
        """);

    public static JsonObject Nations() => Parse("""
        {
          "nations": [
            { "id": "red", "name": "Rotland", "color": "#FF0000" },
            { "id": "blue", "name": "Blauland", "color": "#0000ff" }
          ]
        }
        """);

    public static JsonObject Map() => Parse("""
        {
          "provinces": [
            {
              "id": "a", "name": "A", "resource": "wood", "size": 1, "owner": "red",
              "neighbors": ["b"], "outline": [[0, 0], [10, 0], [10, 10], [0, 10]], "label": [5, 5]
            },
            {
              "id": "b", "name": "B", "resource": "fish", "size": 2, "owner": "blue",
              "neighbors": ["c", "a"], "outline": [[10, 0], [20, 0], [20, 10], [10, 10]], "label": [15, 5]
            },
            {
              "id": "c", "name": "C", "resource": "wood", "size": 3, "owner": "blue",
              "neighbors": ["b"], "outline": [[20, 0], [30, 0], [30, 10], [20, 10]], "label": [25, 5]
            }
          ]
        }
        """);

    public static JsonObject Economy() => Parse("""
        {
          "intervalTicks": 4,
          "taxPerProvince": 2.5,
          "startMoney": 100,
          "startResources": { "wood": 10, "fish": 0.5 },
          "factoryCycleIntervals": 1
        }
        """);

    public static JsonObject Buildings() => Parse("""
        {
          "buildings": [
            {
              "id": "mine", "name": "Mine", "productionBonusPercent": 10,
              "levels": [
                { "money": 10, "resources": { "wood": 2 }, "buildTicks": 3 },
                { "money": 20, "resources": { "wood": 4, "fish": 0.5 }, "buildTicks": 5 }
              ]
            },
            {
              "id": "railworks", "name": "Walzwerk",
              "recipe": { "inputs": { "wood": 2, "fish": 0.5 }, "output": "rails", "amount": 1 },
              "levels": [
                { "money": 30, "buildTicks": 2 },
                { "money": 45, "buildTicks": 2 }
              ]
            },
            {
              "id": "carpentry", "name": "Schreinerei",
              "recipe": { "inputs": { "wood": 3 }, "output": "rails", "amount": 1 },
              "levels": [
                { "money": 30, "buildTicks": 2 }
              ]
            }
          ]
        }
        """);

    public static JsonObject Market() => Parse("""
        {
          "priceChangePercentPerUnit": 10,
          "recoveryPercentPerInterval": 50,
          "minPricePercent": 20,
          "sellPricePercent": 80
        }
        """);

    /// <summary>Alle Datendateien; nicht übergebene Teile werden mit den Standard-Testdaten gefüllt.</summary>
    public static Dictionary<string, string> Files(
        JsonObject? simulation = null,
        JsonObject? resources = null,
        JsonObject? nations = null,
        JsonObject? map = null,
        JsonObject? economy = null,
        JsonObject? buildings = null,
        JsonObject? market = null) => new(StringComparer.Ordinal)
    {
        [GameDataLoader.SimulationFileName] = (simulation ?? Simulation()).ToJsonString(),
        [GameDataLoader.ResourcesFileName] = (resources ?? Resources()).ToJsonString(),
        [GameDataLoader.NationsFileName] = (nations ?? Nations()).ToJsonString(),
        [GameDataLoader.MapFileName] = (map ?? Map()).ToJsonString(),
        [GameDataLoader.EconomyFileName] = (economy ?? Economy()).ToJsonString(),
        [GameDataLoader.BuildingsFileName] = (buildings ?? Buildings()).ToJsonString(),
        [GameDataLoader.MarketFileName] = (market ?? Market()).ToJsonString(),
    };

    public static GameData Load() => GameDataLoader.Load(Files());

    /// <summary>Gibt den Provinz-Eintrag mit der lesbaren ID aus einem Map-Objekt zurück.</summary>
    public static JsonObject Province(JsonObject map, string id) =>
        map["provinces"]!.AsArray().Select(p => p!.AsObject()).Single(p => (string?)p["id"] == id);

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();
}
