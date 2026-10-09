namespace Game.Core.Commands;

/// <summary>Ein eingeplanter Command mit Ausführungstick, absendender Nation und Sequenznummer.</summary>
/// <param name="Issuer">Absendende Nation. Wird von der Session gesetzt, nie vom Client übernommen.</param>
/// <param name="Sequence">Fortlaufende Nummer der Session; bestimmt die Reihenfolge innerhalb einer Nation und eines Ticks.</param>
public sealed record CommandEnvelope(long ExecuteAtTick, NationId Issuer, long Sequence, Command Command);
