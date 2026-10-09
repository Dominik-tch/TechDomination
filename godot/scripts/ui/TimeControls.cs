using Godot;

namespace TechDomination.UI;

/// <summary>
/// Pause/Weiter und Geschwindigkeitsstufen. Leertaste schaltet die Pause um.
/// Die Regeln (jeder darf pausieren und fortsetzen, nur der Host ändert das Tempo) prüft die Session.
/// </summary>
public partial class TimeControls : HBoxContainer
{
    private Label _statusLabel = null!;
    private Button _pauseButton = null!;
    private readonly List<(string Key, Button Button)> _speedButtons = [];
    private SimulationDriver _driver = null!;

    public override void _Ready()
    {
        _statusLabel = GetNode<Label>("StatusLabel");
        _pauseButton = GetNode<Button>("PauseButton");
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");

        _pauseButton.Pressed += TogglePause;

        if (_driver.Data is not { } data)
        {
            return;
        }

        var group = new ButtonGroup();
        var speedContainer = GetNode<HBoxContainer>("SpeedButtons");
        foreach (var level in data.SpeedLevels)
        {
            var button = new Button
            {
                Text = level.Name,
                ToggleMode = true,
                ButtonGroup = group,
                FocusMode = FocusModeEnum.None,
            };
            string key = level.Key;
            button.Pressed += () => SetSpeed(key);
            speedContainer.AddChild(button);
            _speedButtons.Add((key, button));
        }
    }

    public override void _Process(double delta)
    {
        if (_driver.Session is not { } session)
        {
            return;
        }

        _pauseButton.Text = session.IsPaused ? "Weiter" : "Pause";
        _statusLabel.Text = session.PausedBy switch
        {
            null => session.Speed.Name,
            { } player when player == _driver.LocalPlayer => "Pausiert",
            { } player => $"Pausiert von Spieler {player.Value}",
        };

        bool isHost = session.Host == _driver.LocalPlayer;
        foreach (var (key, button) in _speedButtons)
        {
            button.SetPressedNoSignal(key == session.Speed.Key);
            button.Disabled = !isHost;
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Keycode: Key.Space, Pressed: true, Echo: false })
        {
            TogglePause();
            GetViewport().SetInputAsHandled();
        }
    }

    private void TogglePause()
    {
        if (_driver.Session is not { } session)
        {
            return;
        }

        var result = session.IsPaused ? session.Resume(_driver.LocalPlayer) : session.Pause(_driver.LocalPlayer);
        if (!result.IsValid)
        {
            GD.PushWarning(result.Reason);
        }
    }

    private void SetSpeed(string key)
    {
        if (_driver.Session?.SetSpeed(_driver.LocalPlayer, key) is { IsValid: false } result)
        {
            GD.PushWarning(result.Reason);
        }
    }
}
