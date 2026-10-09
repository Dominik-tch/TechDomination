using Game.Core;
using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Session;
using Godot;

namespace TechDomination.UI;

/// <summary>
/// Gebäude der ausgewählten Provinz: Stufen, laufende Baustelle mit Fortschritt, Bauen/Ausbauen und Abbrechen.
/// Ob eine Aktion möglich ist, entscheidet <see cref="Command.Validate"/>; der Grund erscheint als Tooltip.
/// </summary>
public partial class BuildingsSection : VBoxContainer
{
    private sealed record Row(BuildingId Building, Label Status, Button BuildButton, Label CostLabel);

    private readonly List<Row> _rows = [];
    private Control _constructionBox = null!;
    private Label _constructionLabel = null!;
    private ProgressBar _constructionProgress = null!;
    private Button _cancelButton = null!;
    private SimulationDriver _driver = null!;

    /// <summary>Die angezeigte Provinz, oder <c>null</c>.</summary>
    public ProvinceId? Province { get; set; }

    public override void _Ready()
    {
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");
        if (_driver.Data is not { } data)
        {
            return;
        }

        foreach (var building in data.Buildings)
        {
            var line = new HBoxContainer();
            var status = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var button = new Button { FocusMode = FocusModeEnum.None };
            var id = building.Id;
            button.Pressed += () => SubmitForProvince(province => new BuildBuildingCommand(province, id));
            line.AddChild(status);
            line.AddChild(button);
            AddChild(line);

            var cost = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
            cost.AddThemeFontSizeOverride("font_size", 12);
            AddChild(cost);

            _rows.Add(new Row(id, status, button, cost));
        }

        _constructionBox = new VBoxContainer();
        _constructionLabel = new Label();
        _constructionProgress = new ProgressBar { CustomMinimumSize = new Vector2(0, 12), ShowPercentage = false };
        _cancelButton = new Button { Text = "Bau abbrechen", FocusMode = FocusModeEnum.None };
        _cancelButton.Pressed += () => SubmitForProvince(province => new CancelConstructionCommand(province));
        _constructionBox.AddChild(_constructionLabel);
        _constructionBox.AddChild(_constructionProgress);
        _constructionBox.AddChild(_cancelButton);
        AddChild(_constructionBox);
    }

    public override void _Process(double delta)
    {
        if (Province is not { } id || _driver.Session is not { } session)
        {
            return;
        }

        var state = session.State;
        var data = session.Data;
        var province = state.GetProvince(id);
        bool isOwn = province.Owner == _driver.LocalNation;

        // Eingereichte, aber noch nicht ausgeführte Commands (z. B. während der Pause) – sonst wirkt ein Klick wie verloren.
        bool hasPendingAction = session.PendingCommands.Any(e => e.Issuer == _driver.LocalNation && e.Command switch
        {
            BuildBuildingCommand build => build.Province == id,
            CancelConstructionCommand cancel => cancel.Province == id,
            _ => false,
        });

        foreach (var row in _rows)
        {
            UpdateRow(row, data.GetBuilding(row.Building), id, session, isOwn, hasPendingAction);
        }

        UpdateConstruction(data, session, province.Construction, isOwn, hasPendingAction);
    }

    private void UpdateRow(Row row, BuildingDefinition building, ProvinceId province, GameSession session, bool isOwn, bool hasPendingAction)
    {
        int level = session.State.GetProvince(province).GetBuildingLevel(building.Id);
        row.Status.Text = $"{building.Name}: Stufe {level}/{building.MaxLevel}";

        bool canGrow = level < building.MaxLevel;
        row.BuildButton.Visible = isOwn && canGrow;
        row.CostLabel.Visible = isOwn && canGrow;
        if (!row.BuildButton.Visible)
        {
            return;
        }

        var next = building.Level(level + 1);
        row.BuildButton.Text = level == 0 ? "Bauen" : $"Ausbauen auf {level + 1}";
        row.CostLabel.Text = $"Kosten: {FormatCosts(next, session.Data)} · Bauzeit {UiFormat.Duration(next.BuildTicks, session)}";

        var validation = new BuildBuildingCommand(province, building.Id).Validate(session.State, session.Data, _driver.LocalNation);
        row.BuildButton.Disabled = hasPendingAction || !validation.IsValid;
        row.BuildButton.TooltipText = hasPendingAction ? "Wird im nächsten Tick ausgeführt." : validation.Reason ?? "";
    }

    private void UpdateConstruction(GameData data, GameSession session, Game.Core.State.ConstructionState? construction, bool isOwn, bool hasPendingAction)
    {
        _constructionBox.Visible = construction is not null || (isOwn && hasPendingAction);
        if (construction is null)
        {
            _constructionLabel.Text = "Auftrag wird im nächsten Tick ausgeführt …";
            _constructionProgress.Visible = false;
            _cancelButton.Visible = false;
            return;
        }

        var building = data.GetBuilding(construction.Building);
        int total = building.Level(construction.TargetLevel).BuildTicks;
        _constructionLabel.Text =
            $"Bau: {building.Name} Stufe {construction.TargetLevel} – noch {UiFormat.Duration(construction.RemainingTicks, session)}";
        _constructionProgress.Visible = true;
        _constructionProgress.Value = 100.0 * (total - construction.RemainingTicks) / total;
        _cancelButton.Visible = isOwn;
        _cancelButton.Disabled = hasPendingAction;
    }

    private void SubmitForProvince(Func<ProvinceId, Command> createCommand)
    {
        if (Province is { } province)
        {
            ((ICommandSink)_driver).Submit(createCommand(province));
        }
    }

    private static string FormatCosts(BuildingLevelDefinition cost, GameData data)
    {
        var parts = new List<string> { $"{UiFormat.Money(cost.Money)} Geld" };
        foreach (var resource in data.Resources)
        {
            long amount = cost.Resources[resource.Id.Value];
            if (amount > 0)
            {
                parts.Add($"{UiFormat.Resource(amount)} {resource.Name}");
            }
        }

        return string.Join(", ", parts);
    }
}
