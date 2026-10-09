using Game.Core;
using Godot;

namespace TechDomination.UI;

/// <summary>
/// Aufteilen einer Armee: je Einheitentyp ein Schieberegler, wie viele in die neue Armee gehen.
/// „OK“ übergibt die Auswahl; danach wird das Ziel der neuen Armee gewählt.
/// </summary>
public partial class SplitDialog : PanelContainer
{
    private sealed record Row(UnitTypeId Type, HSlider Slider, Label Value, int Available);

    private readonly List<Row> _rows = [];
    private VBoxContainer _rowContainer = null!;
    private Button _okButton = null!;
    private Label _summaryLabel = null!;
    private SimulationDriver _driver = null!;

    /// <summary>Anzahl je Einheitentyp für die neue Armee.</summary>
    public event Action<int[]>? Confirmed;

    public override void _Ready()
    {
        _rowContainer = GetNode<VBoxContainer>("Content/Rows");
        _okButton = GetNode<Button>("Content/Buttons/OkButton");
        _summaryLabel = GetNode<Label>("Content/SummaryLabel");
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");

        _okButton.Pressed += Confirm;
        GetNode<Button>("Content/Buttons/CancelButton").Pressed += Close;
        Visible = false;
    }

    public void Open(ArmyId armyId)
    {
        if (_driver.Session is not { } session || session.State.FindArmy(armyId) is not { } army)
        {
            return;
        }

        foreach (var child in _rowContainer.GetChildren())
        {
            child.QueueFree();
        }

        _rows.Clear();
        foreach (var unitType in session.Data.UnitTypes.Where(u => army.GetUnits(u.Id) > 0))
        {
            int available = army.GetUnits(unitType.Id);
            var line = new HBoxContainer();
            line.AddChild(new Label { Text = unitType.Name, CustomMinimumSize = new Vector2(200, 0) });
            var slider = new HSlider
            {
                MinValue = 0,
                MaxValue = available,
                Step = 1,
                Value = available / 2,
                CustomMinimumSize = new Vector2(180, 0),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.None,
            };
            var value = new Label { CustomMinimumSize = new Vector2(60, 0), HorizontalAlignment = HorizontalAlignment.Right };
            slider.ValueChanged += _ => UpdateSummary();
            line.AddChild(slider);
            line.AddChild(value);
            _rowContainer.AddChild(line);
            _rows.Add(new Row(unitType.Id, slider, value, available));
        }

        UpdateSummary();
        Visible = true;
    }

    public void Close() => Visible = false;

    private void UpdateSummary()
    {
        int moving = 0;
        int total = 0;
        foreach (var row in _rows)
        {
            int count = (int)row.Slider.Value;
            row.Value.Text = $"{count} / {row.Available}";
            moving += count;
            total += row.Available;
        }

        _okButton.Disabled = moving == 0 || moving == total;
        _summaryLabel.Text = moving == 0
            ? "Mindestens eine Einheit muss in die neue Armee."
            : moving == total
                ? "Mindestens eine Einheit muss in der bisherigen Armee bleiben."
                : $"Neue Armee: {moving} Einheiten · bleiben: {total - moving}";
    }

    private void Confirm()
    {
        if (_driver.Data is not { } data)
        {
            return;
        }

        var units = new int[data.UnitTypes.Count];
        foreach (var row in _rows)
        {
            units[row.Type.Value] = (int)row.Slider.Value;
        }

        Visible = false;
        Confirmed?.Invoke(units);
    }
}
