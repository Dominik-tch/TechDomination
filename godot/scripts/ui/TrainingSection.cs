using Game.Core;
using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Session;
using Godot;

namespace TechDomination.UI;

/// <summary>
/// Ausbildung in der ausgewählten eigenen Provinz: automatische Infanterie pro Tag, Aufträge für ausbildbare Einheiten
/// und die Schlange mit Fortschritt und Abbrechen. Gründe aus <see cref="Command.Validate"/> erscheinen als Tooltip.
/// </summary>
public partial class TrainingSection : VBoxContainer
{
    private static readonly int[] OrderSizes = [1, 5];

    private sealed record OrderRow(UnitTypeId Type, Button[] Buttons, Label Cost);

    private readonly List<OrderRow> _orderRows = [];
    private Label _dailyLabel = null!;
    private VBoxContainer _queueBox = null!;
    private string _queueSignature = "";
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

        AddChild(new Label { Text = "Ausbildung:" });
        _dailyLabel = SmallLabel();
        AddChild(_dailyLabel);

        foreach (var unitType in data.UnitTypes.Where(u => u.IsTrainable))
        {
            var line = new HBoxContainer();
            line.AddChild(new Label { Text = unitType.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill });
            var type = unitType.Id;
            var buttons = OrderSizes.Select(count =>
            {
                var button = new Button { Text = $"+{count}", FocusMode = FocusModeEnum.None };
                button.Pressed += () => Submit(province => new TrainUnitsCommand(province, type, count));
                line.AddChild(button);
                return button;
            }).ToArray();
            AddChild(line);

            var cost = SmallLabel();
            AddChild(cost);
            _orderRows.Add(new OrderRow(type, buttons, cost));
        }

        _queueBox = new VBoxContainer();
        AddChild(_queueBox);
    }

    public override void _Process(double delta)
    {
        if (Province is not { } id || _driver.Session is not { } session)
        {
            return;
        }

        var province = session.State.GetProvince(id);
        Visible = province.Owner == _driver.LocalNation;
        if (!Visible)
        {
            return;
        }

        UpdateDaily(session, id);
        foreach (var row in _orderRows)
        {
            UpdateOrderRow(row, session, id);
        }

        UpdateQueue(session, id);
    }

    private void UpdateDaily(GameSession session, ProvinceId id)
    {
        var data = session.Data;
        var province = session.State.GetProvince(id);
        int bonus = data.Buildings.Where(b => province.GetBuildingLevel(b.Id) > 0).Sum(b => b.DailyUnitsBonus);
        long untilNextDay = data.TicksPerDay - session.State.Tick % data.TicksPerDay;

        var automatic = data.UnitTypes
            .Where(u => u.DailyPerProvince > 0)
            .Select(u => $"{u.Name}: +{u.DailyPerProvince + bonus} pro Tag");
        _dailyLabel.Text = $"{string.Join(", ", automatic)} · nächster Tag in {UiFormat.Duration(untilNextDay, session)}";
    }

    private void UpdateOrderRow(OrderRow row, GameSession session, ProvinceId province)
    {
        var unitType = session.Data.GetUnitType(row.Type);
        string requires = unitType.RequiredBuilding is { } building ? $" · braucht {session.Data.GetBuilding(building).Name}" : "";
        row.Cost.Text = $"Kosten: {UiFormat.Costs(unitType.CostMoney, unitType.CostResources, session.Data)} · Dauer {UiFormat.Duration(unitType.TrainingTicks, session)}{requires}";

        for (int i = 0; i < OrderSizes.Length; i++)
        {
            var validation = new TrainUnitsCommand(province, row.Type, OrderSizes[i]).Validate(session.State, session.Data, _driver.LocalNation);
            row.Buttons[i].Disabled = !validation.IsValid;
            row.Buttons[i].TooltipText = validation.Reason ?? $"{OrderSizes[i]} {unitType.Name} in Ausbildung geben";
        }
    }

    // Die Schlange wird nur neu aufgebaut, wenn sie sich ändert; der Fortschritt läuft jeden Frame.
    private void UpdateQueue(GameSession session, ProvinceId id)
    {
        var province = session.State.GetProvince(id);
        string signature = $"{id.Value}:{string.Join(",", province.TrainingQueue.Select(t => t.Value))}";
        if (signature != _queueSignature)
        {
            _queueSignature = signature;
            RebuildQueue(session.Data, province.TrainingQueue);
        }

        if (province.TrainingQueue.Count == 0)
        {
            return;
        }

        var current = session.Data.GetUnitType(province.TrainingQueue[0]);
        var first = _queueBox.GetChild(0);
        first.GetNode<Label>("Line/Name").Text =
            $"{current.Name} – noch {UiFormat.Duration(province.TrainingRemainingTicks, session)}";
        first.GetNode<ProgressBar>("Progress").Value =
            100.0 * (current.TrainingTicks - province.TrainingRemainingTicks) / current.TrainingTicks;
    }

    private void RebuildQueue(GameData data, IReadOnlyList<UnitTypeId> queue)
    {
        foreach (var child in _queueBox.GetChildren())
        {
            _queueBox.RemoveChild(child);
            child.QueueFree();
        }

        for (int i = 0; i < queue.Count; i++)
        {
            var entry = new VBoxContainer();
            var line = new HBoxContainer { Name = "Line" };
            line.AddChild(new Label
            {
                Name = "Name",
                Text = i == 0 ? data.GetUnitType(queue[i]).Name : $"{data.GetUnitType(queue[i]).Name} – wartet",
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
            });
            int index = i;
            var cancel = new Button { Text = "✕", TooltipText = "Abbrechen (volle Erstattung)", FocusMode = FocusModeEnum.None };
            cancel.Pressed += () => Submit(province => new CancelTrainingCommand(province, index));
            line.AddChild(cancel);
            entry.AddChild(line);

            if (i == 0)
            {
                entry.AddChild(new ProgressBar { Name = "Progress", CustomMinimumSize = new Vector2(0, 10), ShowPercentage = false });
            }

            _queueBox.AddChild(entry);
        }
    }

    private void Submit(Func<ProvinceId, Command> createCommand)
    {
        if (Province is { } province)
        {
            ((ICommandSink)_driver).Submit(createCommand(province));
        }
    }

    private static Label SmallLabel()
    {
        var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        label.AddThemeFontSizeOverride("font_size", 12);
        return label;
    }
}
