using System.Text.Json.Serialization;
using Game.Core.Commands;
using Game.Core.State;

namespace Game.Core.Save;

/// <summary>
/// Kompletter Spielstand des Hosts (siehe docs/architecture.md, Abschnitt 8).
/// Wird über <see cref="Session.GameSession.CreateSaveGame"/> erzeugt und über <see cref="SaveGameSerializer"/> geschrieben.
/// </summary>
public sealed class SaveGame
{
    /// <summary>Aktuelle Formatversion. Spielstände mit anderer Version werden abgelehnt.</summary>
    public const int CurrentFormatVersion = 1;

    [JsonConstructor]
    internal SaveGame(int formatVersion, string dataHash, GameState state, SessionSnapshot session, HostState host)
    {
        ArgumentNullException.ThrowIfNull(dataHash);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(host);

        FormatVersion = formatVersion;
        DataHash = dataHash;
        State = state;
        Session = session;
        Host = host;
    }

    public int FormatVersion { get; }

    /// <summary>Hash des Regelwerks, mit dem gespielt wurde (<see cref="Data.GameData.ContentHash"/>).</summary>
    public string DataHash { get; }

    public GameState State { get; }

    public SessionSnapshot Session { get; }

    /// <summary>Daten, die nur der Host kennt und die nie an Clients gehen.</summary>
    public HostState Host { get; }
}

/// <summary>Der Teil der Session, der zum Fortsetzen gebraucht wird.</summary>
/// <param name="SpeedLevel">Lesbare ID der Geschwindigkeitsstufe.</param>
/// <param name="NextSequence">Nächste Sequenznummer für Commands.</param>
/// <param name="PendingCommands">Commands, die auf den nächsten Tick warten (z. B. während der Pause eingereicht).</param>
public sealed record SessionSnapshot(string SpeedLevel, long NextSequence, IReadOnlyList<CommandEnvelope> PendingCommands);

/// <summary>
/// Host-only-Zustand, der nicht im <see cref="GameState"/> liegt. Ab M10 kommt hier der KI-Zustand inklusive
/// KI-Zufallsgenerator hinein (siehe docs/decisions/0002).
/// </summary>
public sealed record HostState;
