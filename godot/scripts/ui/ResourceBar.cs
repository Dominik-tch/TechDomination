using Game.Core;
using Game.Core.Data;
using Game.Core.Economy;
using Godot;

namespace TechDomination.UI;

/// <summary>Zeigt Geld und Basis-Rohstoffe der eigenen Nation: Bestand und Einnahme pro Minute bei der aktuellen Geschwindigkeit.</summary>
public partial class ResourceBar : HBoxContainer
{
    private const int IconSize = 24;

    private readonly List<(ResourceId Id, Label Label)> _resourceLabels = [];
    private Label _moneyLabel = null!;
    private SimulationDriver _driver = null!;

    public override void _Ready()
    {
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");
        if (_driver.Data is not { } data)
        {
            return;
        }

        _moneyLabel = AddEntry(Icons.Money, "Geld");
        foreach (var resource in data.Resources.Where(r => r.Tier == ResourceTier.Basic))
        {
            _resourceLabels.Add((resource.Id, AddEntry(Icons.Resource(resource.Key), resource.Name)));
        }
    }

    public override void _Process(double delta)
    {
        if (_driver.Session is not { } session)
        {
            return;
        }

        var nation = session.State.GetNation(_driver.LocalNation);
        var income = EconomyRules.IncomePerInterval(session.State, session.Data, _driver.LocalNation);

        long moneyPerMinute = UiFormat.PerMinute(income.Money, session);
        _moneyLabel.Text = $"{UiFormat.Money(nation.Money)} ({UiFormat.Signed(UiFormat.Money(moneyPerMinute), moneyPerMinute)}/min)";

        foreach (var (id, label) in _resourceLabels)
        {
            long perMinute = UiFormat.PerMinute(income.Resources[id.Value], session);
            label.Text = $"{UiFormat.Resource(nation.GetResource(id))} ({UiFormat.Signed(UiFormat.Resource(perMinute), perMinute)}/min)";
        }
    }

    private Label AddEntry(Texture2D? icon, string tooltip)
    {
        var entry = new HBoxContainer { TooltipText = tooltip };
        entry.AddChild(new TextureRect
        {
            Texture = icon,
            CustomMinimumSize = new Vector2(IconSize, IconSize),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Pass,
        });

        var label = new Label { MouseFilter = MouseFilterEnum.Pass };
        entry.AddChild(label);
        AddChild(entry);
        return label;
    }
}
