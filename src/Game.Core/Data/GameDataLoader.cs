using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Game.Core.Data;

/// <summary>
/// Lädt und validiert das Regelwerk. Kennt keine Pfade: Der Aufrufer übergibt den Inhalt
/// der Dateien (Dateiname relativ zu godot/data/ → Inhalt), siehe docs/decisions/0003.
/// </summary>
public static class GameDataLoader
{
    public const string SimulationFileName = "simulation.json";

    // Technische Obergrenze, kein Balancing-Wert.
    private const int MaxTicksPerSecond = 1000;

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
        int ticksPerSecond = simulation.TicksPerSecond
            ?? throw new GameDataException($"{SimulationFileName}: Feld 'ticksPerSecond' fehlt.");

        if (ticksPerSecond is < 1 or > MaxTicksPerSecond)
        {
            throw new GameDataException(
                $"{SimulationFileName}: 'ticksPerSecond' muss zwischen 1 und {MaxTicksPerSecond} liegen, ist aber {ticksPerSecond}.");
        }

        return new GameData(ticksPerSecond, ComputeContentHash(files));
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

        return result ?? throw new GameDataException($"{fileName}: Datei ist leer (null).");
    }

    private sealed class SimulationFile
    {
        public int? TicksPerSecond { get; set; }
    }
}
