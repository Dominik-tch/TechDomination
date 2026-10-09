using System.Text.Json;

namespace Game.Core.Serialization;

/// <summary>Gemeinsame JSON-Einstellungen für Spielzustand, Spielstände und Commands.</summary>
internal static class CoreJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Converters = { new CommandJsonConverter() },
    };
}
