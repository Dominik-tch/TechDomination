using Game.Core.Commands;

namespace Game.Core.Events;

/// <summary>Ereignis aus einem Tick, z. B. für Benachrichtigungen in der UI. Flüchtig, nicht Teil des Spielzustands.</summary>
public abstract record GameEvent(long Tick);

/// <summary>Ein Gebäude wurde fertiggestellt.</summary>
/// <param name="Owner">Besitzer der Provinz bei Fertigstellung, z. B. um nur eigene Meldungen anzuzeigen.</param>
public sealed record BuildingCompleted(long Tick, ProvinceId Province, BuildingId Building, int Level, NationId Owner)
    : GameEvent(Tick);

/// <summary>Eine ankommende Armee wurde mit einer dort stehenden eigenen Armee zusammengeführt.</summary>
/// <param name="Absorbed">Die angekommene Armee; sie existiert danach nicht mehr.</param>
/// <param name="Into">Die stehende Armee, die alle Einheiten übernommen hat.</param>
public sealed record ArmiesMerged(long Tick, ArmyId Absorbed, ArmyId Into) : GameEvent(Tick);

/// <summary>Einheiten sind in einer Stadt erschienen – per Auftrag ausgebildet oder automatisch zum Tageswechsel.</summary>
public sealed record UnitsTrained(long Tick, ProvinceId Province, UnitTypeId UnitType, int Count, NationId Owner, bool Automatic)
    : GameEvent(Tick);

/// <summary>Der Unterhalt einer Nation konnte in diesem Wirtschaftstakt nicht vollständig bezahlt werden.</summary>
public sealed record UpkeepShortage(long Tick, NationId Nation) : GameEvent(Tick);

/// <summary>Ein Command wurde bei der Ausführung abgelehnt.</summary>
public sealed record CommandRejected(long Tick, CommandEnvelope Envelope, string Reason) : GameEvent(Tick);
