using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Commands;

/// <summary>
/// Basis aller Spieleraktionen. Spieler, KI und Netzwerk erzeugen nur Commands; nur die Simulation führt sie aus
/// (siehe docs/decisions/0004).
/// </summary>
public abstract record Command
{
    /// <summary>
    /// Prüft, ob die Nation den Command im aktuellen Zustand ausführen darf. Liest nur.
    /// UI und KI nutzen das zur Vorab-Prüfung; die Simulation prüft vor der Ausführung erneut.
    /// Muss mit beliebigen Eingaben umgehen (z. B. unbekannte IDs → ungültig mit Grund, keine Exception).
    /// </summary>
    public abstract ValidationResult Validate(GameState state, GameData data, NationId issuer);

    /// <summary>Führt den Command aus. Wird nur von <see cref="Simulation.Step"/> nach erfolgreicher Validierung aufgerufen.</summary>
    internal abstract void Apply(GameState state, GameData data, NationId issuer);
}
