using Godot;
using TechDomination.Map;
using TechDomination.UI;

namespace TechDomination;

/// <summary>Hauptszene: verbindet Karte, Kamera und Info-Panel.</summary>
public partial class Main : Node2D
{
    private MapView _mapView = null!;
    private ProvinceInfoPanel _infoPanel = null!;

    public override void _Ready()
    {
        _mapView = GetNode<MapView>("MapView");
        _infoPanel = GetNode<ProvinceInfoPanel>("Hud/ProvinceInfoPanel");

        _mapView.SelectionChanged += _infoPanel.ShowProvince;
        GetNode<MapCamera>("MapCamera").FitTo(_mapView.Bounds);
    }

    public override void _ExitTree()
    {
        _mapView.SelectionChanged -= _infoPanel.ShowProvince;
    }
}
