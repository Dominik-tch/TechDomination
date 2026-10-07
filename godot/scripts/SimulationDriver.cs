using Game.Core;
using Game.Core.Data;
using Game.Core.State;
using Godot;

namespace TechDomination;

/// <summary>
/// Autoload: lädt das Regelwerk aus res://data/, hält den Spielzustand und rechnet Echtzeit in Ticks um.
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

    public GameData? Data { get; private set; }

    public GameState? State { get; private set; }

    public override void _Ready()
    {
        try
        {
            Data = GameDataLoader.Load(ReadDataFiles());
        }
        catch (GameDataException e)
        {
            GD.PushError($"Spieldaten konnten nicht geladen werden: {e.Message}");
            SetProcess(false);
            return;
        }

        State = GameStateFactory.CreateNew(Data, DefaultSeed);
    }

    public override void _Process(double delta)
    {
        if (Data is null || State is null)
        {
            return;
        }

        _pendingTicks += delta * Data.TicksPerSecond;
        int ticks = (int)_pendingTicks;
        _pendingTicks -= ticks;

        for (int i = 0; i < Math.Min(ticks, MaxTicksPerFrame); i++)
        {
            Simulation.Step(State, Data);
        }
    }

    public override void _ExitTree()
    {
        if (State is not null)
        {
            GD.Print($"SimulationDriver beendet bei Tick {State.Tick}.");
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
