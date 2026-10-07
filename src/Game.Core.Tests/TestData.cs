using System.Text.Json.Nodes;
using Game.Core.Data;

namespace Game.Core.Tests;

/// <summary>
/// Kleine Test-Datensätze, unabhängig von den echten Dateien in godot/data/.
/// Die Builder liefern veränderbare JSON-Objekte, damit Tests gezielt einzelne Felder ändern können.
/// </summary>
/// <remarks>
/// Testkarte: drei Provinzen in einer Reihe, a | b | c, je 10×10 Einheiten.
/// a gehört "red", b und c gehören "blue".
/// </remarks>
internal static class TestData
{
    public static JsonObject Simulation(int ticksPerSecond = 10) => new() { ["ticksPerSecond"] = ticksPerSecond };

    public static JsonObject Resources() => Parse("""
        {
          "resources": [
            { "id": "wood", "name": "Holz", "tier": "basic" },
            { "id": "fish", "name": "Fisch", "tier": "basic" },
            { "id": "rails", "name": "Schienen", "tier": "advanced" }
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

    /// <summary>Alle Datendateien; nicht übergebene Teile werden mit den Standard-Testdaten gefüllt.</summary>
    public static Dictionary<string, string> Files(
        JsonObject? simulation = null,
        JsonObject? resources = null,
        JsonObject? nations = null,
        JsonObject? map = null) => new(StringComparer.Ordinal)
    {
        [GameDataLoader.SimulationFileName] = (simulation ?? Simulation()).ToJsonString(),
        [GameDataLoader.ResourcesFileName] = (resources ?? Resources()).ToJsonString(),
        [GameDataLoader.NationsFileName] = (nations ?? Nations()).ToJsonString(),
        [GameDataLoader.MapFileName] = (map ?? Map()).ToJsonString(),
    };

    public static GameData Load() => GameDataLoader.Load(Files());

    /// <summary>Gibt den Provinz-Eintrag mit der lesbaren ID aus einem Map-Objekt zurück.</summary>
    public static JsonObject Province(JsonObject map, string id) =>
        map["provinces"]!.AsArray().Select(p => p!.AsObject()).Single(p => (string?)p["id"] == id);

    private static JsonObject Parse(string json) => JsonNode.Parse(json)!.AsObject();
}
