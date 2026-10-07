using Godot;

namespace TechDomination;

/// <summary>Startbildschirm für M0: zeigt den aktuellen Tick. Wird ab M1 durch die Karte ersetzt.</summary>
public partial class MainScreen : Control
{
    private Label _tickLabel = null!;
    private SimulationDriver _driver = null!;

    public override void _Ready()
    {
        _tickLabel = GetNode<Label>("TickLabel");
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");
    }

    public override void _Process(double delta)
    {
        _tickLabel.Text = _driver.State is { } state ? $"Tick: {state.Tick}" : "Spieldaten fehlen";
    }
}
