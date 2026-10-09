using Game.Core;
using Game.Core.Data;
using Game.Core.Map;
using Godot;
using TechDomination.UI;

namespace TechDomination.Map;

/// <summary>
/// Zeichnet die Provinzen in der Farbe ihres Besitzers und wählt Provinzen per Linksklick aus.
/// Kartenkoordinaten aus map.json entsprechen 1:1 den lokalen Koordinaten dieses Nodes.
/// </summary>
public partial class MapView : Node2D
{
    private const float BorderWidth = 2f;
    private const float SelectionWidth = 5f;
    private const float LabelWidth = 200f;
    private const float LabelHeight = 24f;
    private const float ResourceIconSize = 28f;
    private const float ResourceIconOffset = 26f;

    private static readonly Color BorderColor = new(0.12f, 0.12f, 0.12f);
    private static readonly Color SelectionColor = new(1f, 0.85f, 0.2f);

    private readonly List<Polygon2D> _areas = [];
    private Color[] _nationColors = [];
    private Line2D _selectionOutline = null!;
    private SimulationDriver _driver = null!;

    /// <summary>Wird ausgelöst, wenn eine Provinz ausgewählt oder die Auswahl aufgehoben wird (<c>null</c>).</summary>
    public event Action<ProvinceId?>? SelectionChanged;

    public ProvinceId? SelectedProvince { get; private set; }

    /// <summary>Umgebendes Rechteck aller Provinzen in lokalen Koordinaten.</summary>
    public Rect2 Bounds { get; private set; }

    public override void _Ready()
    {
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");
        if (_driver.Data is not { } data)
        {
            return;
        }

        _nationColors = data.Nations.Select(n => Color.FromHtml(n.Color)).ToArray();

        foreach (var province in data.Provinces)
        {
            AddProvince(province);
        }

        _selectionOutline = new Line2D
        {
            Width = SelectionWidth,
            DefaultColor = SelectionColor,
            Closed = true,
            Visible = false,
            ZIndex = 1,
        };
        AddChild(_selectionOutline);

        Bounds = ComputeBounds(data);
    }

    public override void _Process(double delta)
    {
        if (_driver.State is not { } state)
        {
            return;
        }

        // Besitzer können sich später durch Eroberung ändern; einfaches Nachfärben reicht bei einigen hundert Provinzen.
        for (int i = 0; i < _areas.Count; i++)
        {
            _areas[i].Color = _nationColors[state.Provinces[i].Owner.Value];
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click || _driver.Data is not { } data)
        {
            return;
        }

        Vector2 local = ((InputEventMouseButton)MakeInputLocal(click)).Position;
        var point = new MapPoint(Mathf.FloorToInt(local.X), Mathf.FloorToInt(local.Y));
        Select(MapGeometry.FindProvinceAt(data, point));
        GetViewport().SetInputAsHandled();
    }

    private void Select(ProvinceId? id)
    {
        SelectedProvince = id;

        if (id is { } selected && _driver.Data is { } data)
        {
            _selectionOutline.Points = ToVectors(data.GetProvince(selected).Outline);
            _selectionOutline.Visible = true;
        }
        else
        {
            _selectionOutline.Visible = false;
        }

        SelectionChanged?.Invoke(id);
    }

    private void AddProvince(ProvinceDefinition province)
    {
        Vector2[] outline = ToVectors(province.Outline);

        var area = new Polygon2D { Polygon = outline };
        AddChild(area);
        _areas.Add(area);

        AddChild(new Line2D
        {
            Points = outline,
            Closed = true,
            Width = BorderWidth,
            DefaultColor = BorderColor,
        });

        var label = new Label
        {
            Text = province.Name,
            Position = new Vector2(province.LabelPosition.X - LabelWidth / 2, province.LabelPosition.Y - LabelHeight / 2),
            Size = new Vector2(LabelWidth, LabelHeight),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 2,
        };
        label.AddThemeColorOverride("font_color", Colors.White);
        label.AddThemeColorOverride("font_outline_color", Colors.Black);
        label.AddThemeConstantOverride("outline_size", 4);
        AddChild(label);

        // Rohstoff-Symbol unter dem Namen.
        if (Icons.Resource(_driver.Data!.GetResource(province.Resource).Key) is { } icon)
        {
            AddChild(new Sprite2D
            {
                Texture = icon,
                Position = new Vector2(province.LabelPosition.X, province.LabelPosition.Y + ResourceIconOffset),
                Scale = Vector2.One * (ResourceIconSize / icon.GetWidth()),
                ZIndex = 2,
            });
        }
    }

    private static Vector2[] ToVectors(IReadOnlyList<MapPoint> points) =>
        points.Select(p => new Vector2(p.X, p.Y)).ToArray();

    private static Rect2 ComputeBounds(GameData data)
    {
        var points = data.Provinces.SelectMany(p => p.Outline).ToList();
        int minX = points.Min(p => p.X);
        int minY = points.Min(p => p.Y);
        int maxX = points.Max(p => p.X);
        int maxY = points.Max(p => p.Y);
        return new Rect2(minX, minY, maxX - minX, maxY - minY);
    }
}
