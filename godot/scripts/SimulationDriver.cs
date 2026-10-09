using Game.Core;
using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Session;
using Game.Core.State;
using Godot;

namespace TechDomination;

/// <summary>
/// Autoload: lädt das Regelwerk aus res://data/, hält die Session und rechnet Echtzeit in Ticks um.
/// Läuft nur beim Host bzw. im Einzelspieler (siehe docs/architecture.md, Abschnitt 4.2).
/// </summary>
public partial class SimulationDriver : Node
{
    private const string DataDirectory = "res://data";

    // Platzhalter, bis das Spielstart-Setup (M9) den Seed festlegt.
    private const ulong DefaultSeed = 1;

    // Nach einem Hänger wird der Rückstand verworfen statt beliebig viele Ticks nachzuholen.
    private const int MaxTicksPerFrame = 100;

    private double _pendingTicks;

    public GameSession? Session { get; private set; }

    /// <summary>Der Spieler an diesem Rechner. Im Einzelspieler ist er zugleich der Host.</summary>
    public PlayerId LocalPlayer { get; } = new(0);

    public GameData? Data => Session?.Data;

    public GameState? State => Session?.State;

    public override void _Ready()
    {
        GameData data;
        try
        {
            data = GameDataLoader.Load(ReadDataFiles());
        }
        catch (GameDataException e)
        {
            GD.PushError($"Spieldaten konnten nicht geladen werden: {e.Message}");
            SetProcess(false);
            return;
        }

        Session = new GameSession(data, GameStateFactory.CreateNew(data, DefaultSeed), host: LocalPlayer);
    }

    public override void _Process(double delta)
    {
        if (Session is null)
        {
            return;
        }

        if (Session.IsPaused)
        {
            // Während der Pause keine Zeit ansammeln, sonst springt das Spiel beim Fortsetzen.
            _pendingTicks = 0;
            return;
        }

        _pendingTicks += delta * Session.Speed.TicksPerSecond;
        int ticks = (int)_pendingTicks;
        _pendingTicks -= ticks;

        for (int i = 0; i < Math.Min(ticks, MaxTicksPerFrame); i++)
        {
            ReportEvents(Session.Advance());
        }
    }

    public override void _ExitTree()
    {
        if (State is not null)
        {
            GD.Print($"SimulationDriver beendet bei Tick {State.Tick}.");
        }
    }

    // Bis es Benachrichtigungen in der UI gibt, landen abgelehnte Commands im Log.
    private static void ReportEvents(IReadOnlyList<GameEvent> events)
    {
        foreach (var rejected in events.OfType<CommandRejected>())
        {
            GD.PushWarning($"Command abgelehnt (Tick {rejected.Tick}): {rejected.Envelope.Command} – {rejected.Reason}");
        }
    }

    private static Dictionary<string, string> ReadDataFiles()
    {
        var files = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string fileName in DirAccess.GetFilesAt(DataDirectory))
        {
            if (fileName.EndsWith(".json", StringComparison.Ordinal))
            {
                files[fileName] = Godot.FileAccess.GetFileAsString($"{DataDirectory}/{fileName}");
            }
        }

        return files;
    }
}
