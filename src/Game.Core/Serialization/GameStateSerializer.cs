using System.Text.Json;
using Game.Core.State;

namespace Game.Core.Serialization;

/// <summary>
/// (De-)Serialisiert den Spielzustand als JSON. Grundlage für Spielstände, Netzwerk-Snapshots und Zustands-Hashes.
/// Derselbe Zustand ergibt immer exakt dieselben Bytes.
/// </summary>
public static class GameStateSerializer
{
    private static JsonSerializerOptions Options => CoreJson.Options;

    public static byte[] SerializeToUtf8Bytes(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.SerializeToUtf8Bytes(state, Options);
    }

    public static void Serialize(GameState state, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(stream);
        JsonSerializer.Serialize(stream, state, Options);
    }

    public static GameState Deserialize(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<GameState>(utf8Json, Options) ?? throw EmptyState();

    public static GameState Deserialize(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        return JsonSerializer.Deserialize<GameState>(stream, Options) ?? throw EmptyState();
    }

    private static JsonException EmptyState() => new("Der Spielzustand ist leer (null).");
}
