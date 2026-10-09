using Game.Core;
using Game.Core.Commands;
using Game.Core.Map;
using Game.Core.Military;
using Godot;

namespace TechDomination.UI;

/// <summary>Zeigt die ausgewählte Armee: Besitzer, Zusammensetzung und Status. Befehle gibt es in der Befehlsleiste.</summary>
public partial class ArmyPanel : PanelContainer
{
    private Label _titleLabel = null!;
    private Label _unitsLabel = null!;
    private Label _statusLabel = null!;
    private Label _hintLabel = null!;
    private SimulationDriver _driver = null!;
    private ArmyId? _army;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("Content/TitleLabel");
        _unitsLabel = GetNode<Label>("Content/UnitsLabel");
        _statusLabel = GetNode<Label>("Content/StatusLabel");
        _hintLabel = GetNode<Label>("Content/HintLabel");
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");

        Visible = false;
    }

    public void ShowArmy(ArmyId? army)
    {
        _army = army;
        Visible = army is not null;
    }

    public override void _Process(double delta)
    {
        if (!Visible || _army is not { } id || _driver.Session is not { } session || session.State.FindArmy(id) is not { } army)
        {
            return;
        }

        var data = session.Data;
        bool isOwn = army.Owner == _driver.LocalNation;

        _titleLabel.Text = $"Armee von {data.GetNation(army.Owner).Name}";
        _unitsLabel.Text = string.Join("\n", data.UnitTypes
            .Where(u => army.GetUnits(u.Id) > 0)
            .Select(u => $"{u.Name}: {army.GetUnits(u.Id)} ({army.GetStrength(u.Id) / 100} %)"));

        string place = data.GetProvince(ArmyRules.ProvinceOf(data, army)).Name;
        bool hasPendingOrder = session.PendingCommands.Any(e => e.Command switch
        {
            MoveArmyCommand move => move.Army == id,
            HaltArmyCommand halt => halt.Army == id,
            _ => false,
        });

        if (hasPendingOrder)
        {
            _statusLabel.Text = "Befehl wird im nächsten Tick ausgeführt …";
        }
        else if (army.IsMoving)
        {
            var lastLeg = army.Legs[^1];
            var destination = data.Graph.Normalize(new PathPosition(lastLeg.From, lastLeg.To, lastLeg.StopAt));
            string target = destination.City is { } city
                ? $"nach {data.GetProvince(city).Name}"
                : $"zum Weg {data.GetProvince(lastLeg.From).Name} – {data.GetProvince(lastLeg.To).Name}";
            _statusLabel.Text = $"Marschiert {target} – Ankunft in {UiFormat.Duration(ArmyRules.RemainingTicks(data, army), session)}";
        }
        else
        {
            _statusLabel.Text = army.Position.IsAtCity ? $"Steht in der Stadt {place}" : $"Steht auf dem Weg in {place}";
        }

        _hintLabel.Visible = isOwn;
    }
}
