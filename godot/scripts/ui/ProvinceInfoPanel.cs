using Game.Core;
using Game.Core.Economy;
using Godot;

namespace TechDomination.UI;

/// <summary>Zeigt Name, Besitzer, Rohstoff und Gebäude der ausgewählten Provinz.</summary>
public partial class ProvinceInfoPanel : PanelContainer
{
    private Label _nameLabel = null!;
    private Label _ownerLabel = null!;
    private Label _resourceLabel = null!;
    private BuildingsSection _buildingsSection = null!;
    private TrainingSection _trainingSection = null!;
    private SimulationDriver _driver = null!;
    private ProvinceId? _province;

    public override void _Ready()
    {
        _nameLabel = GetNode<Label>("Content/NameLabel");
        _ownerLabel = GetNode<Label>("Content/OwnerLabel");
        _resourceLabel = GetNode<Label>("Content/ResourceLabel");
        _buildingsSection = GetNode<BuildingsSection>("Content/BuildingsSection");
        _trainingSection = GetNode<TrainingSection>("Content/TrainingSection");
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");
        Visible = false;
    }

    public void ShowProvince(ProvinceId? province)
    {
        _province = province;
        _buildingsSection.Province = province;
        _trainingSection.Province = province;
        Visible = province is not null;
        Refresh();
    }

    // Der Besitzer kann sich ändern, solange das Panel offen ist.
    public override void _Process(double delta) => Refresh();

    private void Refresh()
    {
        if (_province is not { } id || _driver.Session is not { } session)
        {
            return;
        }

        var data = session.Data;
        var state = session.State;

        var province = data.GetProvince(id);
        _nameLabel.Text = province.Name;
        _ownerLabel.Text = $"Besitzer: {data.GetNation(state.GetProvince(id).Owner).Name}";
        long perMinute = UiFormat.PerMinute(EconomyRules.ProductionPerInterval(state, data, province), session);
        _resourceLabel.Text = $"Rohstoff: {data.GetResource(province.Resource).Name} ({UiFormat.Signed(UiFormat.Resource(perMinute), perMinute)}/min)";
    }
}
