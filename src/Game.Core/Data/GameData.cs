namespace Game.Core.Data;

/// <summary>Unveränderliches Regelwerk, geladen aus den Dateien in godot/data/.</summary>
public sealed class GameData
{
    internal GameData(int ticksPerSecond, string contentHash)
    {
        TicksPerSecond = ticksPerSecond;
        ContentHash = contentHash;
    }

    /// <summary>
    /// Ticks pro Sekunde bei Normalgeschwindigkeit. Nur die Darstellungsschicht nutzt den Wert,
    /// um Echtzeit in Ticks umzurechnen; die Simulation selbst kennt keine Sekunden.
    /// </summary>
    public int TicksPerSecond { get; }

    /// <summary>SHA-256 über alle Datendateien. Erkennt abweichende Regelwerke bei Spielständen und Netzwerk-Beitritt.</summary>
    public string ContentHash { get; }
}
