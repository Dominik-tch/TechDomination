using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Save;
using Game.Core.State;

namespace Game.Core.Session;

/// <summary>
/// Läuft beim Host: hält Zustand und Regelwerk, sammelt Commands für den nächsten Tick und verwaltet
/// Pause und Geschwindigkeit (siehe docs/architecture.md, Abschnitt 6). Kennt keine Echtzeit;
/// die Darstellungsschicht ruft <see cref="Advance"/> passend zur Geschwindigkeit auf.
/// </summary>
public sealed class GameSession
{
    private readonly List<CommandEnvelope> _pending = [];
    private long _nextSequence;

    public GameSession(GameData data, GameState state, PlayerId host)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(state);

        Data = data;
        State = state;
        Host = host;
        Speed = data.DefaultSpeedLevel;
    }

    public GameData Data { get; }

    public GameState State { get; }

    /// <summary>Der Spieler, der die Partie hostet. Nur er darf die Geschwindigkeit ändern.</summary>
    public PlayerId Host { get; }

    public SpeedLevelDefinition Speed { get; private set; }

    /// <summary>Wer pausiert hat, oder <c>null</c>, wenn das Spiel läuft.</summary>
    public PlayerId? PausedBy { get; private set; }

    public bool IsPaused => PausedBy is not null;

    /// <summary>Commands, die im nächsten Tick ausgeführt werden.</summary>
    public IReadOnlyList<CommandEnvelope> PendingCommands => _pending;

    /// <summary>
    /// Plant einen Command für den nächsten Tick ein. Geprüft wird erst bei der Ausführung;
    /// für eine Vorab-Prüfung <see cref="Command.Validate"/> verwenden.
    /// </summary>
    public CommandEnvelope Submit(NationId issuer, Command command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var envelope = new CommandEnvelope(State.Tick, issuer, _nextSequence++, command);
        _pending.Add(envelope);
        return envelope;
    }

    /// <summary>Führt einen Tick mit allen eingeplanten Commands aus. Während der Pause passiert nichts.</summary>
    public IReadOnlyList<GameEvent> Advance()
    {
        if (IsPaused)
        {
            return [];
        }

        var commands = _pending.ToList();
        _pending.Clear();
        return Simulation.Step(State, Data, commands);
    }

    /// <summary>
    /// Erzeugt einen Spielstand aus dem aktuellen Zustand. Verweist auf den laufenden Zustand,
    /// muss also sofort geschrieben werden (<see cref="SaveGameSerializer.Write"/>).
    /// </summary>
    public SaveGame CreateSaveGame() => new(
        SaveGame.CurrentFormatVersion,
        Data.ContentHash,
        State,
        new SessionSnapshot(Speed.Key, _nextSequence, _pending.ToList()),
        new HostState());

    /// <summary>
    /// Setzt eine Partie aus einem Spielstand fort. Nach dem Laden ist das Spiel pausiert (durch den Host).
    /// </summary>
    /// <exception cref="SaveGameException">Der Spielstand passt nicht zu den Spieldaten.</exception>
    public static GameSession FromSaveGame(GameData data, SaveGame save, PlayerId host)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(save);

        if (save.DataHash != data.ContentHash)
        {
            throw new SaveGameException("Der Spielstand wurde mit anderen Spieldaten erstellt und kann nicht geladen werden.");
        }

        var session = new GameSession(data, save.State, host)
        {
            Speed = data.FindSpeedLevel(save.Session.SpeedLevel)
                ?? throw new SaveGameException($"Unbekannte Geschwindigkeitsstufe '{save.Session.SpeedLevel}' im Spielstand."),
            PausedBy = host,
            _nextSequence = save.Session.NextSequence,
        };
        session._pending.AddRange(save.Session.PendingCommands);
        return session;
    }

    /// <summary>Jeder Spieler darf pausieren.</summary>
    public ValidationResult Pause(PlayerId player)
    {
        if (IsPaused)
        {
            return ValidationResult.Invalid("Das Spiel ist bereits pausiert.");
        }

        PausedBy = player;
        return ValidationResult.Valid;
    }

    /// <summary>Jeder Spieler darf die Pause aufheben.</summary>
    public ValidationResult Resume(PlayerId player)
    {
        if (!IsPaused)
        {
            return ValidationResult.Invalid("Das Spiel ist nicht pausiert.");
        }

        PausedBy = null;
        return ValidationResult.Valid;
    }

    /// <summary>Nur der Host darf die Geschwindigkeit ändern.</summary>
    public ValidationResult SetSpeed(PlayerId player, string speedLevelKey)
    {
        if (player != Host)
        {
            return ValidationResult.Invalid("Nur der Host darf die Geschwindigkeit ändern.");
        }

        if (Data.FindSpeedLevel(speedLevelKey) is not { } level)
        {
            return ValidationResult.Invalid($"Unbekannte Geschwindigkeitsstufe '{speedLevelKey}'.");
        }

        Speed = level;
        return ValidationResult.Valid;
    }
}
