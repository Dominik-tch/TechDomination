using Game.Core;
using Game.Core.Commands;
using Game.Core.Events;
using Game.Core.Map;
using Game.Core.Session;
using Game.Core.State;
using Godot;
using TechDomination.UI;

namespace TechDomination.Map;

/// <summary>
/// Zeichnet die Armeen als Marker auf dem Wegenetz und nimmt Befehle für die ausgewählte Armee entgegen.
/// <list type="bullet">
/// <item>Linksklick auf einen Marker wählt die Armee aus.</item>
/// <item>„Bewegen“ bzw. das Ziel nach „Aufteilen“: gelbe Vorschau zur Mausposition, Linksklick bestätigt,
/// Rechtsklick oder Esc bricht ab. Das Ziel rastet an Städten und eigenen stehenden Armeen ein.</item>
/// <item>Rechtsklick ohne aktiven Befehl ist eine Abkürzung für einen sofortigen Marschbefehl.</item>
/// </list>
/// Die bestätigte Route der ausgewählten Armee ist weiß-gelb gestrichelt, die Vorschau gelb; beide enden mit einem Pfeil.
/// </summary>
public partial class ArmyLayer : Node2D
{
    private const float MarkerRadius = 10f;
    private const float StackOffset = 14f;
    private const float RouteWidth = 3f;
    private const float ArrowLength = 12f;
    private const float ArrowHalfWidth = 7f;

    // Ein Ziel höchstens so weit (Karteneinheiten) neben einer Stadt oder einer eigenen stehenden Armee rastet dort ein.
    private const long SnapRadius = 15;

    private static readonly Color MarkerOutline = new(0.05f, 0.05f, 0.05f);
    private static readonly Color Yellow = new(1f, 0.85f, 0.2f);
    private static readonly Color White = new(1f, 1f, 1f);

    private enum OrderMode
    {
        None,
        Move,
        SplitTarget,
    }

    private readonly Dictionary<ArmyId, Node2D> _markers = [];
    private Color[] _nationColors = [];
    private Line2D _confirmedRoute = null!;
    private Polygon2D _confirmedArrow = null!;
    private Line2D _previewRoute = null!;
    private Polygon2D _previewArrow = null!;
    private Line2D _selectionRing = null!;
    private SimulationDriver _driver = null!;
    private Notifications? _notifications;

    private OrderMode _mode;
    private int[]? _splitUnits;
    private Vector2 _hoverPosition;

    /// <summary>Wird ausgelöst, wenn eine Armee ausgewählt oder die Auswahl aufgehoben wird (<c>null</c>).</summary>
    public event Action<ArmyId?>? SelectionChanged;

    /// <summary>Hinweis für die Befehlsleiste, solange ein Ziel gewählt wird; <c>null</c> = kein Hinweis.</summary>
    public event Action<string?>? HintChanged;

    public ArmyId? SelectedArmy { get; private set; }

    public override void _Ready()
    {
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");
        _notifications = GetNodeOrNull<Notifications>("../Hud/Notifications");
        _driver.EventsRaised += OnEvents;
        if (_driver.Data is { } data)
        {
            _nationColors = data.Nations.Select(n => Color.FromHtml(n.Color)).ToArray();
        }

        _confirmedRoute = new Line2D
        {
            Width = RouteWidth,
            Texture = DashTexture(),
            TextureMode = Line2D.LineTextureMode.Tile,
            TextureRepeat = TextureRepeatEnum.Enabled,
            ZIndex = 3,
            Visible = false,
        };
        _confirmedArrow = new Polygon2D { Color = Yellow, ZIndex = 3, Visible = false };
        _previewRoute = new Line2D { Width = RouteWidth, DefaultColor = Yellow, ZIndex = 4, Visible = false };
        _previewArrow = new Polygon2D { Color = Yellow, ZIndex = 4, Visible = false };
        _selectionRing = new Line2D
        {
            Points = Circle(MarkerRadius + 4f),
            Closed = true,
            Width = 3f,
            DefaultColor = Yellow,
            ZIndex = 6,
            Visible = false,
        };
        AddChild(_confirmedRoute);
        AddChild(_confirmedArrow);
        AddChild(_previewRoute);
        AddChild(_previewArrow);
        AddChild(_selectionRing);
    }

    public override void _ExitTree()
    {
        _driver.EventsRaised -= OnEvents;
    }

    public void ClearSelection()
    {
        if (SelectedArmy is not null)
        {
            Select(null);
        }
    }

    /// <summary>„Bewegen“: Ziel für die ausgewählte Armee mit der Maus wählen.</summary>
    public void BeginMove() => BeginOrder(OrderMode.Move, null, "Ziel wählen – Klick bestätigt, Rechtsklick bricht ab");

    /// <summary>Nach „Aufteilen“: Ziel für die neue Armee wählen. Abbruch lässt die Armee unverändert zusammen.</summary>
    public void BeginSplitTarget(int[] units) =>
        BeginOrder(OrderMode.SplitTarget, units, "Ziel der neuen Armee wählen – Rechtsklick bricht ab, die Armee bleibt zusammen");

    public void HaltSelected()
    {
        if (SelectedArmy is { } army)
        {
            ((ICommandSink)_driver).Submit(new HaltArmyCommand(army));
        }
    }

    public override void _Process(double delta)
    {
        if (_driver.Session is not { } session)
        {
            return;
        }

        SyncMarkers(session.State, session.Data.Graph);

        if (SelectedArmy is { } id && session.State.FindArmy(id) is { } army)
        {
            _selectionRing.Position = _markers[id].Position;
            _selectionRing.Visible = true;
            ShowConfirmedRoute(army, session.Data.Graph);
            ShowPreview(session, army);
        }
        else
        {
            if (SelectedArmy is not null)
            {
                Select(null);
            }

            _selectionRing.Visible = false;
            SetRouteVisible(_confirmedRoute, _confirmedArrow, false);
            SetRouteVisible(_previewRoute, _previewArrow, false);
        }
    }

    public override void _Input(InputEvent @event)
    {
        // Esc bricht einen laufenden Befehl ab, bevor das Pausenmenü sie bekommt.
        if (_mode != OrderMode.None && @event is InputEventKey { Keycode: Key.Escape, Pressed: true, Echo: false })
        {
            CancelOrder();
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (_driver.Session is not { } session)
        {
            return;
        }

        if (_mode != OrderMode.None)
        {
            HandleOrderInput(session, @event);
            return;
        }

        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click:
                if (ArmyAt(LocalPosition(click)) is { } army)
                {
                    Select(army);
                    GetViewport().SetInputAsHandled();
                }

                break;

            // Die Kamera lässt einen Rechtsklick ohne Ziehen durch; er gilt als sofortiger Marschbefehl.
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false } release when SelectedArmy is { } selected:
                Submit(session, new MoveArmyCommand(selected, TargetAt(session, LocalPosition(release), selected)));
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private void HandleOrderInput(GameSession session, InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseMotion motion:
                _hoverPosition = LocalPosition(motion);
                return;

            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click when SelectedArmy is { } army:
                var target = TargetAt(session, LocalPosition(click), army);
                Command command = _mode == OrderMode.SplitTarget
                    ? new SplitArmyCommand(army, _splitUnits!, target)
                    : new MoveArmyCommand(army, target);
                if (Submit(session, command))
                {
                    EndOrder();
                }

                break;

            case InputEventMouseButton { ButtonIndex: MouseButton.Right }:
                // Erst beim Loslassen abbrechen, damit das Loslassen nicht noch einen Marschbefehl auslöst.
                if (!((InputEventMouseButton)@event).Pressed)
                {
                    CancelOrder();
                }

                break;

            default:
                return;
        }

        GetViewport().SetInputAsHandled();
    }

    private void BeginOrder(OrderMode mode, int[]? splitUnits, string hint)
    {
        if (SelectedArmy is null)
        {
            return;
        }

        _mode = mode;
        _splitUnits = splitUnits;
        _hoverPosition = GetLocalMousePosition();
        HintChanged?.Invoke(hint);
    }

    private void CancelOrder() => EndOrder();

    private void EndOrder()
    {
        _mode = OrderMode.None;
        _splitUnits = null;
        HintChanged?.Invoke(null);
    }

    private bool Submit(GameSession session, Command command)
    {
        var validation = command.Validate(session.State, session.Data, _driver.LocalNation);
        if (!validation.IsValid)
        {
            _notifications?.Show(validation.Reason!);
            return false;
        }

        ((ICommandSink)_driver).Submit(command);
        return true;
    }

    private void Select(ArmyId? army)
    {
        if (_mode != OrderMode.None)
        {
            EndOrder();
        }

        SelectedArmy = army;
        SelectionChanged?.Invoke(army);
    }

    // Wird die ausgewählte Armee mit einer anderen zusammengeführt, bleibt die vereinte Armee ausgewählt.
    private void OnEvents(IReadOnlyList<GameEvent> events)
    {
        foreach (var merged in events.OfType<ArmiesMerged>())
        {
            if (merged.Absorbed == SelectedArmy)
            {
                Select(merged.Into);
            }
        }
    }

    /// <summary>Zielpunkt im Wegenetz: eine eigene stehende Armee in der Nähe, sonst eine Stadt in der Nähe, sonst der nächste Punkt.</summary>
    private PathPosition TargetAt(GameSession session, Vector2 localPosition, ArmyId movingArmy)
    {
        var point = ToFine(localPosition);
        long snap = SnapRadius * FinePoint.Scale;

        var standing = session.State.Armies
            .Where(a => a.Id != movingArmy && a.Owner == _driver.LocalNation && !a.IsMoving)
            .Select(a => (Army: a, Distance: FinePoint.DistanceSquared(point, session.Data.Graph.PointOf(a.Position))))
            .Where(x => x.Distance <= snap * snap)
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Army.Id.Value)
            .FirstOrDefault();

        return standing.Army?.Position ?? session.Data.Graph.NearestPosition(point, snap);
    }

    private void ShowPreview(GameSession session, ArmyState army)
    {
        if (_mode == OrderMode.None)
        {
            SetRouteVisible(_previewRoute, _previewArrow, false);
            return;
        }

        var target = TargetAt(session, _hoverPosition, army.Id);
        if (session.Data.Graph.FindRoute(army.Position, target) is not { } route)
        {
            SetRouteVisible(_previewRoute, _previewArrow, false);
            return;
        }

        var graph = session.Data.Graph;
        var points = new List<Vector2> { ToVector(graph.PointOf(route.Start)) };
        points.AddRange(route.Legs.Select(leg => ToVector(graph.PointOf(new PathPosition(leg.From, leg.To, leg.StopAt)))));
        DrawRoute(_previewRoute, _previewArrow, points);
    }

    private void ShowConfirmedRoute(ArmyState army, MapGraph graph)
    {
        if (!army.IsMoving)
        {
            SetRouteVisible(_confirmedRoute, _confirmedArrow, false);
            return;
        }

        var points = new List<Vector2> { ToVector(graph.PointOf(army.Position)) };
        points.AddRange(army.Legs.Select(leg => ToVector(graph.PointOf(new PathPosition(leg.From, leg.To, leg.StopAt)))));
        DrawRoute(_confirmedRoute, _confirmedArrow, points);
    }

    // Linie bis kurz vor das Ziel, dann ein Pfeil, dessen Spitze genau auf dem Ziel liegt.
    private static void DrawRoute(Line2D line, Polygon2D arrow, List<Vector2> points)
    {
        points = points.Where((p, i) => i == 0 || p.DistanceTo(points[i - 1]) > 0.01f).ToList();
        if (points.Count < 2)
        {
            SetRouteVisible(line, arrow, false);
            return;
        }

        var tip = points[^1];
        var direction = (tip - points[^2]).Normalized();
        var normal = new Vector2(-direction.Y, direction.X);
        var arrowBase = tip - direction * Math.Min(ArrowLength, tip.DistanceTo(points[^2]));

        line.Points = [.. points.Take(points.Count - 1), arrowBase];
        arrow.Polygon = [tip, arrowBase + normal * ArrowHalfWidth, arrowBase - normal * ArrowHalfWidth];
        SetRouteVisible(line, arrow, true);
    }

    private static void SetRouteVisible(Line2D line, Polygon2D arrow, bool visible)
    {
        line.Visible = visible;
        arrow.Visible = visible;
    }

    private ArmyId? ArmyAt(Vector2 localPosition)
    {
        // Oberster Marker zuerst (zuletzt gezeichnet).
        foreach (var (id, marker) in _markers.OrderByDescending(pair => pair.Key.Value))
        {
            if (marker.Position.DistanceTo(localPosition) <= MarkerRadius + 2f)
            {
                return id;
            }
        }

        return null;
    }

    private void SyncMarkers(GameState state, MapGraph graph)
    {
        foreach (var gone in _markers.Keys.Where(id => state.FindArmy(id) is null).ToList())
        {
            _markers[gone].QueueFree();
            _markers.Remove(gone);
        }

        // Armeen am selben Punkt leicht versetzt nebeneinander zeigen.
        var stacked = new Dictionary<FinePoint, int>();
        foreach (var army in state.Armies)
        {
            if (!_markers.TryGetValue(army.Id, out var marker))
            {
                marker = CreateMarker();
                _markers[army.Id] = marker;
            }

            var point = graph.PointOf(army.Position);
            int index = stacked.GetValueOrDefault(point);
            stacked[point] = index + 1;

            marker.Position = ToVector(point) + new Vector2(index * StackOffset, 0);
            marker.GetNode<Polygon2D>("Body").Color = _nationColors[army.Owner.Value];
            marker.GetNode<Label>("Count").Text = army.TotalUnits.ToString();
        }
    }

    private Node2D CreateMarker()
    {
        var marker = new Node2D { ZIndex = 5 };
        marker.AddChild(new Polygon2D { Name = "Body", Polygon = Circle(MarkerRadius) });
        marker.AddChild(new Line2D { Points = Circle(MarkerRadius), Closed = true, Width = 2f, DefaultColor = MarkerOutline });

        var count = new Label
        {
            Name = "Count",
            Position = new Vector2(-MarkerRadius, -MarkerRadius),
            Size = new Vector2(2 * MarkerRadius, 2 * MarkerRadius),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        count.AddThemeFontSizeOverride("font_size", 11);
        count.AddThemeColorOverride("font_color", Colors.White);
        count.AddThemeColorOverride("font_outline_color", Colors.Black);
        count.AddThemeConstantOverride("outline_size", 3);
        marker.AddChild(count);

        AddChild(marker);
        return marker;
    }

    // Abwechselnd gelb und weiß; die Linie kachelt die Textur entlang ihrer Länge.
    private static ImageTexture DashTexture()
    {
        var image = Image.CreateEmpty(32, 2, false, Image.Format.Rgba8);
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 2; y++)
            {
                image.SetPixel(x, y, x < 16 ? Yellow : White);
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private Vector2 LocalPosition(InputEvent @event) => ((InputEventMouse)MakeInputLocal(@event)).Position;

    private static FinePoint ToFine(Vector2 position) =>
        new((long)Math.Round(position.X * FinePoint.Scale), (long)Math.Round(position.Y * FinePoint.Scale));

    private static Vector2 ToVector(FinePoint point) => new((float)point.X / FinePoint.Scale, (float)point.Y / FinePoint.Scale);

    private static Vector2[] Circle(float radius) =>
        Enumerable.Range(0, 20).Select(i => Vector2.FromAngle(i * Mathf.Tau / 20) * radius).ToArray();
}
