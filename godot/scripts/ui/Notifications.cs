using Game.Core.Events;
using Godot;

namespace TechDomination.UI;

/// <summary>Kurze Meldungen unten links zu eigenen Ereignissen (fertige Bauten, abgelehnte Aktionen).</summary>
public partial class Notifications : VBoxContainer
{
    private const double DisplaySeconds = 6;
    private const int MaxEntries = 5;

    private SimulationDriver _driver = null!;

    public override void _Ready()
    {
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");
        _driver.EventsRaised += OnEvents;
    }

    public override void _ExitTree()
    {
        _driver.EventsRaised -= OnEvents;
    }

    private void OnEvents(IReadOnlyList<GameEvent> events)
    {
        if (_driver.Data is not { } data)
        {
            return;
        }

        foreach (var gameEvent in events)
        {
            string? message = gameEvent switch
            {
                BuildingCompleted completed when completed.Owner == _driver.LocalNation =>
                    $"{data.GetBuilding(completed.Building).Name} Stufe {completed.Level} in {data.GetProvince(completed.Province).Name} fertig.",
                CommandRejected rejected when rejected.Envelope.Issuer == _driver.LocalNation =>
                    $"Aktion abgelehnt: {rejected.Reason}",
                _ => null,
            };

            if (message is not null)
            {
                Add(message);
            }
        }
    }

    private void Add(string message)
    {
        var entry = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        entry.AddChild(new Label { Text = message, AutowrapMode = TextServer.AutowrapMode.WordSmart });
        AddChild(entry);

        while (GetChildCount() > MaxEntries)
        {
            var oldest = GetChild(0);
            RemoveChild(oldest);
            oldest.QueueFree();
        }

        GetTree().CreateTimer(DisplaySeconds).Timeout += () =>
        {
            if (IsInstanceValid(entry))
            {
                entry.QueueFree();
            }
        };
    }
}
