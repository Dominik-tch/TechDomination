using System.IO.Compression;
using System.Text.Json;
using Game.Core.Data;
using Game.Core.Serialization;

namespace Game.Core.Save;

/// <summary>Kurzinfo zu einem Spielstand für Auswahllisten, ohne ihn vollständig zu laden.</summary>
public sealed record SaveGameSummary(int FormatVersion, long Tick);

/// <summary>
/// Schreibt und liest Spielstände als GZip-komprimiertes JSON. Kennt keine Pfade, nur Streams.
/// Beim Lesen werden Formatversion und Regelwerk geprüft; passt etwas nicht, wird abgelehnt statt repariert.
/// </summary>
public static class SaveGameSerializer
{
    public static void Write(SaveGame save, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(save);
        ArgumentNullException.ThrowIfNull(stream);

        using var gzip = new GZipStream(stream, CompressionLevel.Optimal, leaveOpen: true);
        JsonSerializer.Serialize(gzip, save, CoreJson.Options);
    }

    /// <summary>Liest einen Spielstand und prüft, ob er zu Version und Regelwerk passt.</summary>
    /// <exception cref="SaveGameException">Datei beschädigt, andere Version oder anderes Regelwerk.</exception>
    public static SaveGame Read(Stream stream, GameData data)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(data);

        using var document = ReadDocument(stream);
        var root = document.RootElement;

        int version = ReadFormatVersion(root)
            ?? throw new SaveGameException("Der Spielstand ist beschädigt (Formatversion fehlt).");
        if (version != SaveGame.CurrentFormatVersion)
        {
            throw new SaveGameException(
                $"Der Spielstand stammt aus einer anderen Spielversion (Format {version}, erwartet {SaveGame.CurrentFormatVersion}).");
        }

        if (!root.TryGetProperty("dataHash", out var hash) || hash.ValueKind != JsonValueKind.String
            || hash.GetString() != data.ContentHash)
        {
            throw new SaveGameException(
                "Der Spielstand wurde mit anderen Spieldaten erstellt und kann nicht geladen werden.");
        }

        SaveGame save;
        try
        {
            save = root.Deserialize<SaveGame>(CoreJson.Options)
                ?? throw new SaveGameException("Der Spielstand ist leer.");
        }
        catch (Exception e) when (e is JsonException or ArgumentException)
        {
            throw new SaveGameException($"Der Spielstand ist beschädigt: {e.Message}", e);
        }

        RequireMatchingStructure(save, data);
        return save;
    }

    /// <summary>Liest nur Formatversion und Tick, z. B. für die Liste im Laden-Menü. <c>null</c>, wenn die Datei beschädigt ist.</summary>
    public static SaveGameSummary? ReadSummary(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        try
        {
            using var document = ReadDocument(stream);
            var root = document.RootElement;
            if (ReadFormatVersion(root) is not { } version
                || !root.TryGetProperty("state", out var state)
                || !state.TryGetProperty("tick", out var tick)
                || !tick.TryGetInt64(out long tickValue))
            {
                return null;
            }

            return new SaveGameSummary(version, tickValue);
        }
        catch (SaveGameException)
        {
            return null;
        }
    }

    private static JsonDocument ReadDocument(Stream stream)
    {
        try
        {
            using var gzip = new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true);
            return JsonDocument.Parse(gzip);
        }
        catch (Exception e) when (e is InvalidDataException or JsonException)
        {
            throw new SaveGameException("Der Spielstand ist beschädigt und kann nicht gelesen werden.", e);
        }
    }

    private static int? ReadFormatVersion(JsonElement root) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty("formatVersion", out var version)
        && version.TryGetInt32(out int value)
            ? value
            : null;

    // Bei gleichem Daten-Hash passt die Struktur immer; das fängt nur manipulierte Dateien ab,
    // bevor sie im Spiel zu Indexfehlern führen.
    private static void RequireMatchingStructure(SaveGame save, GameData data)
    {
        var state = save.State;
        bool matches = state.Provinces.Count == data.Provinces.Count
            && state.Nations.Count == data.Nations.Count
            && state.Nations.All(n => n.Resources.Count == data.Resources.Count
                && n.MarketPrices.Count == data.Resources.Count
                && n.FactoryLimits.Count == data.Buildings.Count
                && n.LastFactoryRuns.Count == data.Buildings.Count)
            && state.Provinces.All(p => data.Contains(p.Owner)
                && p.BuildingLevels.Count == data.Buildings.Count
                && p.BuildingLevels.Select((level, i) => level >= 0 && level <= data.Buildings[i].MaxLevel).All(ok => ok)
                && (p.Construction is not { } c
                    || (data.Contains(c.Building) && c.TargetLevel >= 1 && c.TargetLevel <= data.GetBuilding(c.Building).MaxLevel)))
            && state.Armies.All(a => data.Contains(a.Owner)
                && a.Units.Count == data.UnitTypes.Count
                && a.Units.All(count => count >= 0)
                && data.Graph.IsValid(a.Position)
                && a.Legs.All(leg => data.Graph.AreConnected(leg.From, leg.To)
                    && leg.StopAt > 0 && leg.StopAt <= data.Graph.Length(leg.From, leg.To)))
            && save.Session.SpeedLevel is not null
            && save.Session.PendingCommands is { } pending
            && pending.All(c => c?.Command is not null && c.ExecuteAtTick == state.Tick);

        if (!matches)
        {
            throw new SaveGameException("Der Spielstand ist beschädigt (passt nicht zu den Spieldaten).");
        }
    }
}
