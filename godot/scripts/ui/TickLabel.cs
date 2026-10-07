using Godot;

namespace TechDomination.UI;

/// <summary>Zeigt den aktuellen Tick an.</summary>
public partial class TickLabel : Label
{
    private SimulationDriver _driver = null!;

    public override void _Ready()
    {
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");
    }

    public override void _Process(double delta)
    {
        Text = _driver.State is { } state ? $"Tick: {state.Tick}" : "Spieldaten fehlen";
    }
}
