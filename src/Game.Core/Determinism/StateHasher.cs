using System.Security.Cryptography;
using Game.Core.Serialization;
using Game.Core.State;

namespace Game.Core.Determinism;

/// <summary>Berechnet einen Hash über den Spielzustand, um Zustände in Tests und bei der Fehlersuche zu vergleichen.</summary>
public static class StateHasher
{
    /// <summary>SHA-256 über das serialisierte JSON, als Hex-String.</summary>
    public static string ComputeHash(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return Convert.ToHexString(SHA256.HashData(GameStateSerializer.SerializeToUtf8Bytes(state)));
    }
}
