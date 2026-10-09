using Godot;

namespace TechDomination.Map;

/// <summary>
/// Kartenkamera: Zoom mit dem Mausrad (zum Mauszeiger hin), Verschieben mit gedrückter mittlerer oder rechter Maustaste.
/// Ein Rechtsklick ohne Ziehen wird nicht verbraucht, damit er als Marschbefehl ankommt.
/// </summary>
public partial class MapCamera : Camera2D
{
    private const float ZoomStep = 1.15f;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 4f;
    private const float FitMargin = 0.9f;

    // Ab so vielen Pixeln Mausbewegung gilt ein gedrückter rechter Knopf als Ziehen statt als Klick.
    private const float DragThreshold = 6f;

    private bool _middleDragging;
    private bool _rightPressed;
    private bool _rightDragging;
    private Vector2 _rightPressPosition;

    /// <summary>Zentriert die Kamera auf den Bereich und zoomt so, dass er vollständig sichtbar ist.</summary>
    public void FitTo(Rect2 area)
    {
        if (area.Size.X <= 0 || area.Size.Y <= 0)
        {
            return;
        }

        Vector2 viewport = GetViewportRect().Size;
        float zoom = Mathf.Min(viewport.X / area.Size.X, viewport.Y / area.Size.Y) * FitMargin;
        SetZoomLevel(zoom);
        Position = area.GetCenter();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true } wheel:
                ZoomAt(wheel.Position, ZoomStep);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true } wheel:
                ZoomAt(wheel.Position, 1f / ZoomStep);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Middle } middle:
                _middleDragging = middle.Pressed;
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } press:
                _rightPressed = true;
                _rightDragging = false;
                _rightPressPosition = press.Position;
                return;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }:
                bool wasDragging = _rightDragging;
                _rightPressed = false;
                _rightDragging = false;
                if (!wasDragging)
                {
                    return;
                }

                break;
            case InputEventMouseMotion motion when _middleDragging || _rightPressed:
                if (_rightPressed && !_rightDragging)
                {
                    if (motion.Position.DistanceTo(_rightPressPosition) < DragThreshold)
                    {
                        return;
                    }

                    _rightDragging = true;
                }

                Position -= motion.Relative / Zoom;
                break;
            default:
                return;
        }

        GetViewport().SetInputAsHandled();
    }

    // Der Kartenpunkt unter dem Mauszeiger bleibt beim Zoomen an derselben Bildschirmstelle.
    private void ZoomAt(Vector2 screenPosition, float factor)
    {
        Vector2 offsetFromCenter = screenPosition - GetViewportRect().Size / 2;
        Vector2 pointUnderMouse = Position + offsetFromCenter / Zoom;

        SetZoomLevel(Zoom.X * factor);
        Position = pointUnderMouse - offsetFromCenter / Zoom;
    }

    private void SetZoomLevel(float zoom)
    {
        float clamped = Mathf.Clamp(zoom, MinZoom, MaxZoom);
        Zoom = new Vector2(clamped, clamped);
    }
}
