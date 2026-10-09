using Game.Core.Commands;

namespace Game.Core.Events;

/// <summary>Ereignis aus einem Tick, z. B. für Benachrichtigungen in der UI. Flüchtig, nicht Teil des Spielzustands.</summary>
public abstract record GameEvent(long Tick);

/// <summary>Ein Command wurde bei der Ausführung abgelehnt.</summary>
public sealed record CommandRejected(long Tick, CommandEnvelope Envelope, string Reason) : GameEvent(Tick);
