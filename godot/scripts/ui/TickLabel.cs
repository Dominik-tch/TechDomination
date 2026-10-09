using Godot;

namespace TechDomination.UI;

/// <summary>Zeigt den aktuellen Spieltag und Tick an.</summary>
public partial class TickLabel : Label
{
    private SimulationDriver _driver = null!;

    public override void _Ready()
    {
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");
    }

    public override void _Process(double delta)
    {
        Text = _driver.State is { } state && _driver.Data is { } data
            ? $"Tag {state.Tick / data.TicksPerDay + 1} · Tick {state.Tick}"
            : "Spieldaten fehlen";
    }
}
