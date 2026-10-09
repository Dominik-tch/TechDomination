using Game.Core;
using Godot;

namespace TechDomination.UI;

/// <summary>
/// Befehlsleiste unten in der Mitte für die ausgewählte eigene Armee: Bewegen, Aufteilen, Anhalten.
/// Darüber steht ein Hinweis, solange ein Ziel gewählt wird.
/// </summary>
public partial class ArmyCommandBar : VBoxContainer
{
    private Button _moveButton = null!;
    private Button _splitButton = null!;
    private Button _haltButton = null!;
    private Label _hintLabel = null!;
    private SimulationDriver _driver = null!;
    private ArmyId? _army;

    public event Action? MoveRequested;

    public event Action? SplitRequested;

    public event Action? HaltRequested;

    public override void _Ready()
    {
        _moveButton = GetNode<Button>("Buttons/MoveButton");
        _splitButton = GetNode<Button>("Buttons/SplitButton");
        _haltButton = GetNode<Button>("Buttons/HaltButton");
        _hintLabel = GetNode<Label>("HintLabel");
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");

        _moveButton.Pressed += () => MoveRequested?.Invoke();
        _splitButton.Pressed += () => SplitRequested?.Invoke();
        _haltButton.Pressed += () => HaltRequested?.Invoke();
        Visible = false;
    }

    /// <summary>Zeigt die Leiste für eine Armee (nur bei eigenen) oder blendet sie aus (<c>null</c>).</summary>
    public void ShowFor(ArmyId? army)
    {
        _army = army;
        SetHint(null);
    }

    public void SetHint(string? hint)
    {
        _hintLabel.Text = hint ?? "";
        _hintLabel.Visible = hint is not null;
    }

    public override void _Process(double delta)
    {
        var army = _army is { } id ? _driver.State?.FindArmy(id) : null;
        Visible = army is not null && army.Owner == _driver.LocalNation;
        if (!Visible)
        {
            return;
        }

        _splitButton.Disabled = army!.TotalUnits < 2;
        _splitButton.TooltipText = army.TotalUnits < 2 ? "Aufteilen braucht mindestens 2 Einheiten." : "Aufteilen";
        _haltButton.Disabled = !army.IsMoving;
        _haltButton.TooltipText = army.IsMoving ? "Anhalten" : "Die Armee marschiert nicht.";
    }
}
