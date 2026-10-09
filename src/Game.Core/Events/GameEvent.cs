using Game.Core.Commands;

namespace Game.Core.Events;

/// <summary>Ereignis aus einem Tick, z. B. für Benachrichtigungen in der UI. Flüchtig, nicht Teil des Spielzustands.</summary>
public abstract record GameEvent(long Tick);

/// <summary>Ein Gebäude wurde fertiggestellt.</summary>
/// <param name="Owner">Besitzer der Provinz bei Fertigstellung, z. B. um nur eigene Meldungen anzuzeigen.</param>
public sealed record BuildingCompleted(long Tick, ProvinceId Province, BuildingId Building, int Level, NationId Owner)
    : GameEvent(Tick);

/// <summary>Ein Command wurde bei der Ausführung abgelehnt.</summary>
public sealed record CommandRejected(long Tick, CommandEnvelope Envelope, string Reason) : GameEvent(Tick);
