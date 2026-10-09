using Game.Core;
using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.Session;
using Godot;

namespace TechDomination.UI;

/// <summary>
/// Wirtschaftsfenster (Taste W): Reiter „Markt“ mit allen Ressourcen, Preisen und Kaufen/Verkaufen,
/// Reiter „Fabriken“ mit einem Schieberegler je Fabriktyp, der festlegt, wie viele Fabriken laufen.
/// </summary>
public partial class EconomyWindow : PanelContainer
{
    private static readonly int[] TradeSteps = [1, 10, 100];

    private sealed record MarketRow(
        ResourceId Resource, Label Stock, Label Income, Label BuyPrice, Label SellPrice, Button[] Buy, Button[] Sell);

    private sealed record FactoryRow(BuildingId Factory, HSlider Slider, Label Status);

    private readonly List<MarketRow> _marketRows = [];
    private readonly List<FactoryRow> _factoryRows = [];
    private SimulationDriver _driver = null!;

    // Verhindert, dass das Nachführen des Reglers aus dem Zustand selbst einen Command auslöst.
    private bool _updatingSliders;

    public override void _Ready()
    {
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");
        GetNode<Button>("Content/Header/CloseButton").Pressed += () => Visible = false;
        Visible = false;

        if (_driver.Data is not { } data)
        {
            return;
        }

        BuildMarket(GetNode<GridContainer>("Content/Tabs/Markt/MarketGrid"), data);
        BuildFactories(GetNode<VBoxContainer>("Content/Tabs/Fabriken/FactoryList"), data);
    }

    public void Toggle() => Visible = !Visible;

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Keycode: Key.W, Pressed: true, Echo: false })
        {
            Toggle();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible || _driver.Session is not { } session)
        {
            return;
        }

        UpdateMarket(session);
        UpdateFactories(session);
    }

    private void BuildMarket(GridContainer grid, GameData data)
    {
        string[] headers = ["Ressource", "Bestand", "pro Minute", "Kaufpreis", "Verkaufspreis", "Kaufen", "", "", "Verkaufen", "", ""];
        foreach (string header in headers)
        {
            grid.AddChild(new Label { Text = header });
        }

        foreach (var resource in data.Resources)
        {
            grid.AddChild(new Label { Text = resource.Name });
            var stock = new Label { HorizontalAlignment = HorizontalAlignment.Right };
            var income = new Label { HorizontalAlignment = HorizontalAlignment.Right };
            var buyPrice = new Label { HorizontalAlignment = HorizontalAlignment.Right };
            var sellPrice = new Label { HorizontalAlignment = HorizontalAlignment.Right };
            grid.AddChild(stock);
            grid.AddChild(income);
            grid.AddChild(buyPrice);
            grid.AddChild(sellPrice);

            var id = resource.Id;
            var buy = TradeSteps.Select(amount => TradeButton($"+{amount}", () => Submit(new BuyResourceCommand(id, amount)))).ToArray();
            var sell = TradeSteps.Select(amount => TradeButton($"−{amount}", () => Submit(new SellResourceCommand(id, amount)))).ToArray();
            foreach (var button in buy.Concat(sell))
            {
                grid.AddChild(button);
            }

            _marketRows.Add(new MarketRow(id, stock, income, buyPrice, sellPrice, buy, sell));
        }
    }

    private void BuildFactories(VBoxContainer list, GameData data)
    {
        foreach (var building in data.Buildings.Where(b => b.IsFactory))
        {
            var row = new HBoxContainer();
            var info = new VBoxContainer { CustomMinimumSize = new Vector2(320, 0) };
            info.AddChild(new Label { Text = building.Name });
            var recipe = new Label { Text = UiFormat.Recipe(building.Recipe!, data) };
            recipe.AddThemeFontSizeOverride("font_size", 12);
            info.AddChild(recipe);

            var slider = new HSlider
            {
                MinValue = 0,
                Step = 1,
                CustomMinimumSize = new Vector2(200, 0),
                SizeFlagsVertical = SizeFlags.ShrinkCenter,
                FocusMode = FocusModeEnum.None,
            };
            var id = building.Id;
            slider.ValueChanged += value => OnSliderChanged(id, slider, value);

            var status = new Label { SizeFlagsHorizontal = SizeFlags.ExpandFill, AutowrapMode = TextServer.AutowrapMode.WordSmart };

            row.AddChild(info);
            row.AddChild(slider);
            row.AddChild(status);
            list.AddChild(row);
            list.AddChild(new HSeparator());

            _factoryRows.Add(new FactoryRow(id, slider, status));
        }
    }

    private void UpdateMarket(GameSession session)
    {
        var nation = session.State.GetNation(_driver.LocalNation);
        var income = EconomyRules.IncomePerInterval(session.State, session.Data, _driver.LocalNation);

        foreach (var row in _marketRows)
        {
            long price = nation.GetMarketPrice(row.Resource);
            long perMinute = UiFormat.PerMinute(income.Resources[row.Resource.Value], session);
            row.Stock.Text = UiFormat.Resource(nation.GetResource(row.Resource));
            row.Income.Text = UiFormat.Signed(UiFormat.Resource(perMinute), perMinute);
            row.BuyPrice.Text = UiFormat.Money(price);
            row.SellPrice.Text = UiFormat.Money(MarketRules.SellRevenue(nation, session.Data, row.Resource, 1));

            string name = session.Data.GetResource(row.Resource).Name;
            for (int i = 0; i < TradeSteps.Length; i++)
            {
                int amount = TradeSteps[i];
                ApplyValidation(
                    row.Buy[i],
                    new BuyResourceCommand(row.Resource, amount),
                    session,
                    $"{amount} {name} kaufen für {UiFormat.Money(MarketRules.BuyCost(nation, session.Data, row.Resource, amount))}");
                ApplyValidation(
                    row.Sell[i],
                    new SellResourceCommand(row.Resource, amount),
                    session,
                    $"{amount} {name} verkaufen für {UiFormat.Money(MarketRules.SellRevenue(nation, session.Data, row.Resource, amount))}");
            }
        }
    }

    private void UpdateFactories(GameSession session)
    {
        var nation = session.State.GetNation(_driver.LocalNation);

        _updatingSliders = true;
        foreach (var row in _factoryRows)
        {
            int capacity = FactoryRules.Capacity(session.State, _driver.LocalNation, row.Factory);
            int active = FactoryRules.ActiveFactories(session.State, _driver.LocalNation, row.Factory);

            // Ein eingereichter, noch nicht ausgeführter Reglerwert hat Vorrang, sonst springt der Regler zurück.
            var pending = session.PendingCommands
                .Where(e => e.Issuer == _driver.LocalNation)
                .Select(e => e.Command)
                .OfType<SetFactoryActivityCommand>()
                .LastOrDefault(c => c.Factory == row.Factory);
            int shown = pending is null ? active : Math.Min(pending.ActiveCount ?? capacity, capacity);

            row.Slider.MaxValue = Math.Max(capacity, 1);
            row.Slider.Editable = capacity > 0;
            row.Slider.Value = shown;

            row.Status.Text = capacity == 0 ? "keine Fabrik" : StatusText(session, nation, row.Factory, shown, capacity);
        }

        _updatingSliders = false;
    }

    private static string StatusText(GameSession session, Game.Core.State.NationState nation, BuildingId factory, int active, int capacity)
    {
        string text = $"läuft {active}/{capacity}";
        int lastRuns = nation.LastFactoryRuns[factory.Value];
        if (lastRuns < active)
        {
            text += $" · letzter Durchlauf {lastRuns}×";
            if (FactoryRules.MissingInput(nation, session.Data.GetBuilding(factory).Recipe!) is { } missing)
            {
                text += $", fehlt: {session.Data.GetResource(missing).Name}";
            }
        }

        return text;
    }

    private void OnSliderChanged(BuildingId factory, HSlider slider, double value)
    {
        if (_updatingSliders || _driver.Session is not { } session)
        {
            return;
        }

        int capacity = FactoryRules.Capacity(session.State, _driver.LocalNation, factory);
        int count = (int)value;

        // Ganz rechts = alle, auch künftig gebaute Fabriken.
        Submit(new SetFactoryActivityCommand(factory, count >= capacity ? null : count));
    }

    private void ApplyValidation(Button button, Command command, GameSession session, string tooltipIfValid)
    {
        var validation = command.Validate(session.State, session.Data, _driver.LocalNation);
        button.Disabled = !validation.IsValid;
        button.TooltipText = validation.Reason ?? tooltipIfValid;
    }

    private void Submit(Command command) => ((ICommandSink)_driver).Submit(command);

    private static Button TradeButton(string text, Action onPressed)
    {
        var button = new Button { Text = text, FocusMode = FocusModeEnum.None };
        button.Pressed += onPressed;
        return button;
    }
}
