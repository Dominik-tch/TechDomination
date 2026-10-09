using Game.Core;
using Godot;
using TechDomination.Map;
using TechDomination.UI;

namespace TechDomination;

/// <summary>Hauptszene: verbindet Karte, Armeen, Kamera und Panels. Es ist immer höchstens eine Provinz oder eine Armee ausgewählt.</summary>
public partial class Main : Node2D
{
    private MapView _mapView = null!;
    private ArmyLayer _armyLayer = null!;
    private ProvinceInfoPanel _infoPanel = null!;
    private ArmyPanel _armyPanel = null!;
    private ArmyCommandBar _commandBar = null!;
    private SplitDialog _splitDialog = null!;

    public override void _Ready()
    {
        _mapView = GetNode<MapView>("MapView");
        _armyLayer = GetNode<ArmyLayer>("ArmyLayer");
        _infoPanel = GetNode<ProvinceInfoPanel>("Hud/ProvinceInfoPanel");
        _armyPanel = GetNode<ArmyPanel>("Hud/ArmyPanel");
        _commandBar = GetNode<ArmyCommandBar>("Hud/ArmyCommandBar");
        _splitDialog = GetNode<SplitDialog>("Hud/SplitDialog");

        _mapView.SelectionChanged += OnProvinceSelected;
        _armyLayer.SelectionChanged += OnArmySelected;
        _armyLayer.HintChanged += _commandBar.SetHint;
        _commandBar.MoveRequested += OnMoveRequested;
        _commandBar.SplitRequested += OnSplitRequested;
        _commandBar.HaltRequested += _armyLayer.HaltSelected;
        _splitDialog.Confirmed += _armyLayer.BeginSplitTarget;
        GetNode<Button>("Hud/WindowButtons/EconomyButton").Pressed += GetNode<EconomyWindow>("Hud/EconomyWindow").Toggle;
        GetNode<MapCamera>("MapCamera").FitTo(_mapView.Bounds);
    }

    public override void _ExitTree()
    {
        _mapView.SelectionChanged -= OnProvinceSelected;
        _armyLayer.SelectionChanged -= OnArmySelected;
        _armyLayer.HintChanged -= _commandBar.SetHint;
        _commandBar.MoveRequested -= OnMoveRequested;
        _commandBar.SplitRequested -= OnSplitRequested;
        _commandBar.HaltRequested -= _armyLayer.HaltSelected;
        _splitDialog.Confirmed -= _armyLayer.BeginSplitTarget;
    }

    private void OnMoveRequested()
    {
        _splitDialog.Close();
        _armyLayer.BeginMove();
    }

    private void OnSplitRequested()
    {
        if (_armyLayer.SelectedArmy is { } army)
        {
            _splitDialog.Open(army);
        }
    }

    private void OnProvinceSelected(ProvinceId? province)
    {
        _infoPanel.ShowProvince(province);
        if (province is not null)
        {
            _armyLayer.ClearSelection();
        }
    }

    private void OnArmySelected(ArmyId? army)
    {
        _armyPanel.ShowArmy(army);
        _commandBar.ShowFor(army);
        _splitDialog.Close();
        if (army is not null)
        {
            _mapView.ClearSelection();
        }
    }
}
