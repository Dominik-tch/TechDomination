using Game.Core.Commands;

namespace TechDomination;

/// <summary>
/// Der einzige Weg, wie die UI das Spiel verändert (siehe docs/architecture.md, Abschnitt 7).
/// Am Host bzw. im Einzelspieler landen Commands direkt in der Session, beim Client später im Netzwerk.
/// </summary>
public interface ICommandSink
{
    /// <summary>Reicht einen Command für die eigene Nation ein. Ausgeführt und endgültig geprüft wird er im nächsten Tick.</summary>
    void Submit(Command command);
}
