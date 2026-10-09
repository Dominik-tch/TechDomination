using Game.Core;
using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Session;
using Game.Core.State;
using Godot;

namespace TechDomination.UI;

/// <summary>
/// Gebäude der ausgewählten Provinz: gebaute Gebäude mit Stufe (und Rezept bei Fabriken), Ausbauen,
/// Neubau über eine Auswahl, laufende Baustelle mit Fortschritt und Abbrechen.
/// Ob eine Aktion möglich ist, entscheidet <see cref="Command.Validate"/>; der Grund erscheint als Tooltip.
/// </summary>
public partial class BuildingsSection : VBoxContainer
{
    private sealed record Row(BuildingId Building, Control Container, Label Status, Label Recipe, Button UpgradeButton, Label CostLabel);

    private readonly List<Row> _rows = [];
    private readonly List<BuildingId> _newBuildingChoices = [];
    private Label _noBuildingsLabel = null!;
    private Control _newBuildingBox = null!;
    private OptionButton _newBuildingSelect = null!;
    private Button _newBuildingButton = null!;
    private Label _newBuildingCost = null!;
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

        _noBuildingsLabel = new Label { Text = "keine" };
        AddChild(_noBuildingsLabel);

        foreach (var building in data.Buildings)
        {
            var container = new VBoxContainer();
            var line = new HBoxContainer();
            var status = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            var button = new Button { FocusMode = FocusModeEnum.None };
            var id = building.Id;
            button.Pressed += () => SubmitForProvince(province => new BuildBuildingCommand(province, id));
            line.AddChild(status);
            line.AddChild(button);
            container.AddChild(line);

            var recipe = SmallLabel();
            recipe.Visible = building.Recipe is not null;
            if (building.Recipe is { } r)
            {
                recipe.Text = $"Rezept: {UiFormat.Recipe(r, data)}";
            }

            container.AddChild(recipe);
            var cost = SmallLabel();
            container.AddChild(cost);
            AddChild(container);

            _rows.Add(new Row(id, container, status, recipe, button, cost));
        }

        _newBuildingBox = new VBoxContainer();
        var newLine = new HBoxContainer();
        _newBuildingSelect = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill, FocusMode = FocusModeEnum.None };
        _newBuildingButton = new Button { Text = "Bauen", FocusMode = FocusModeEnum.None };
        _newBuildingButton.Pressed += () =>
        {
            if (SelectedNewBuilding() is { } building)
            {
                SubmitForProvince(province => new BuildBuildingCommand(province, building));
            }
        };
        newLine.AddChild(_newBuildingSelect);
        newLine.AddChild(_newBuildingButton);
        _newBuildingCost = SmallLabel();
        _newBuildingBox.AddChild(newLine);
        _newBuildingBox.AddChild(_newBuildingCost);
        AddChild(_newBuildingBox);

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

        var province = session.State.GetProvince(id);
        bool isOwn = province.Owner == _driver.LocalNation;

        // Eingereichte, aber noch nicht ausgeführte Commands (z. B. während der Pause) – sonst wirkt ein Klick wie verloren.
        bool hasPendingAction = session.PendingCommands.Any(e => e.Issuer == _driver.LocalNation && e.Command switch
        {
            BuildBuildingCommand build => build.Province == id,
            CancelConstructionCommand cancel => cancel.Province == id,
            _ => false,
        });

        bool anyBuilt = false;
        foreach (var row in _rows)
        {
            anyBuilt |= UpdateRow(row, session, id, isOwn, hasPendingAction);
        }

        _noBuildingsLabel.Visible = !anyBuilt;
        UpdateNewBuilding(session, id, isOwn, hasPendingAction);
        UpdateConstruction(session, province.Construction, isOwn, hasPendingAction);
    }

    private bool UpdateRow(Row row, GameSession session, ProvinceId province, bool isOwn, bool hasPendingAction)
    {
        var building = session.Data.GetBuilding(row.Building);
        int level = session.State.GetProvince(province).GetBuildingLevel(row.Building);
        row.Container.Visible = level > 0;
        if (level == 0)
        {
            return false;
        }

        row.Status.Text = $"{building.Name}: Stufe {level}/{building.MaxLevel}";
        bool canGrow = isOwn && level < building.MaxLevel;
        row.UpgradeButton.Visible = canGrow;
        row.CostLabel.Visible = canGrow;
        if (canGrow)
        {
            row.UpgradeButton.Text = $"Ausbauen auf {level + 1}";
            row.CostLabel.Text = CostText(building.Level(level + 1), session);
            ApplyValidation(row.UpgradeButton, new BuildBuildingCommand(province, row.Building), session, hasPendingAction);
        }

        return true;
    }

    private void UpdateNewBuilding(GameSession session, ProvinceId province, bool isOwn, bool hasPendingAction)
    {
        var unbuilt = session.Data.Buildings
            .Where(b => session.State.GetProvince(province).GetBuildingLevel(b.Id) == 0)
            .Select(b => b.Id)
            .ToList();

        _newBuildingBox.Visible = isOwn && unbuilt.Count > 0;
        if (!_newBuildingBox.Visible)
        {
            return;
        }

        if (!unbuilt.SequenceEqual(_newBuildingChoices))
        {
            var previous = SelectedNewBuilding();
            _newBuildingChoices.Clear();
            _newBuildingChoices.AddRange(unbuilt);
            _newBuildingSelect.Clear();
            foreach (var building in unbuilt)
            {
                _newBuildingSelect.AddItem(session.Data.GetBuilding(building).Name);
            }

            int index = previous is { } p ? _newBuildingChoices.IndexOf(p) : -1;
            _newBuildingSelect.Select(Math.Max(index, 0));
        }

        if (SelectedNewBuilding() is { } selected)
        {
            var building = session.Data.GetBuilding(selected);
            _newBuildingCost.Text = building.Recipe is { } recipe
                ? $"{CostText(building.Level(1), session)}\nRezept: {UiFormat.Recipe(recipe, session.Data)}"
                : CostText(building.Level(1), session);
            ApplyValidation(_newBuildingButton, new BuildBuildingCommand(province, selected), session, hasPendingAction);
        }
    }

    private void UpdateConstruction(GameSession session, ConstructionState? construction, bool isOwn, bool hasPendingAction)
    {
        _constructionBox.Visible = construction is not null || (isOwn && hasPendingAction);
        if (construction is null)
        {
            _constructionLabel.Text = "Auftrag wird im nächsten Tick ausgeführt …";
            _constructionProgress.Visible = false;
            _cancelButton.Visible = false;
            return;
        }

        var building = session.Data.GetBuilding(construction.Building);
        int total = building.Level(construction.TargetLevel).BuildTicks;
        _constructionLabel.Text =
            $"Bau: {building.Name} Stufe {construction.TargetLevel} – noch {UiFormat.Duration(construction.RemainingTicks, session)}";
        _constructionProgress.Visible = true;
        _constructionProgress.Value = 100.0 * (total - construction.RemainingTicks) / total;
        _cancelButton.Visible = isOwn;
        _cancelButton.Disabled = hasPendingAction;
    }

    private void ApplyValidation(Button button, Command command, GameSession session, bool hasPendingAction)
    {
        var validation = command.Validate(session.State, session.Data, _driver.LocalNation);
        button.Disabled = hasPendingAction || !validation.IsValid;
        button.TooltipText = hasPendingAction ? "Wird im nächsten Tick ausgeführt." : validation.Reason ?? "";
    }

    private BuildingId? SelectedNewBuilding()
    {
        int index = _newBuildingSelect.Selected;
        return index >= 0 && index < _newBuildingChoices.Count ? _newBuildingChoices[index] : null;
    }

    private void SubmitForProvince(Func<ProvinceId, Command> createCommand)
    {
        if (Province is { } province)
        {
            ((ICommandSink)_driver).Submit(createCommand(province));
        }
    }

    private static string CostText(BuildingLevelDefinition cost, GameSession session) =>
        $"Kosten: {UiFormat.Costs(cost.Money, cost.Resources, session.Data)} · Bauzeit {UiFormat.Duration(cost.BuildTicks, session)}";

    private static Label SmallLabel()
    {
        var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", 12);
        return label;
    }
}
