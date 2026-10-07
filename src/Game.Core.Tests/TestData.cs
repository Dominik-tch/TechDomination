using Game.Core.Data;

namespace Game.Core.Tests;

/// <summary>Kleine Test-Datensätze, unabhängig von den echten Dateien in godot/data/.</summary>
internal static class TestData
{
    public static Dictionary<string, string> Files(int ticksPerSecond = 10) => new(StringComparer.Ordinal)
    {
        [GameDataLoader.SimulationFileName] = $$"""{ "ticksPerSecond": {{ticksPerSecond}} }""",
    };

    public static GameData Load() => GameDataLoader.Load(Files());
}
