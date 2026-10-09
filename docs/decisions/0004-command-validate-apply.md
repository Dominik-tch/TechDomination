# 0004 – Commands bringen Validate und Apply selbst mit

- **Status:** angenommen
- **Datum:** 2026-10-09
- **Betrifft:** Command-System, UI, KI ([architecture.md](../architecture.md), Abschnitt 5)

## Kontext

Die Architektur sah ursprünglich pro Command-Typ eine eigene Handler-Klasse mit `Validate` und `Apply` vor. Das erfordert eine Registrierung (Command-Typ → Handler) und für jeden Command zwei Klassen.

Außerdem müssen UI und KI vorab wissen, ob eine Aktion möglich ist – etwa um einen Bau-Button auszugrauen, „Nicht genug Stahl“ anzuzeigen oder nur gültige Commands zu erzeugen.

## Entscheidung

- `Command` ist ein abstrakter Record. Jeder Command-Typ implementiert `Validate` und `Apply` selbst; es gibt keine separaten Handler und keine Registrierung.
- **`Validate(state, data, issuer)` ist öffentlich.** Es liest den Zustand nur und gibt ein `ValidationResult` zurück: gültig, oder ungültig **mit Grund**.
- **`Apply(state, data, issuer)` ist `internal`** und wird nur von `Simulation.Step()` nach erfolgreicher Validierung aufgerufen.
- `Step()` validiert jeden Command unmittelbar vor der Ausführung erneut. Eine Vorab-Prüfung durch UI oder KI ist nur ein Hinweis, weil sich der Zustand bis zur Ausführung ändern kann.

## Begründung

- **Weniger Boilerplate:** eine Klasse pro Command, keine Registrierung, die man vergessen kann.
- **Vorab-Prüfung für UI und KI:** Dieselbe Logik, die die Simulation verwendet, steht UI und KI zur Verfügung. Es gibt keine zweite, abweichende Prüfung in der UI.
- **Gründe statt true/false:** Die UI kann dem Spieler sagen, *warum* etwas nicht geht; die KI kann Gründe für Debugging protokollieren. Abgelehnte Commands liefern denselben Grund im Event `CommandRejected`.
- **Sicherheit bleibt erhalten:** `Validate` ist lesend und damit unkritisch. Weil `Apply` `internal abstract` ist, kann außerhalb von Core (z. B. in Godot) weder ein eigener Command-Typ definiert noch der Zustand direkt verändert werden.

## Konsequenzen

- `Validate` darf den Zustand nie verändern und muss mit beliebigen Eingaben umgehen können (z. B. unbekannte Provinz-ID → ungültig mit Grund, keine Exception).
- Die Gründe sind vorerst deutsche Texte. Falls später Übersetzungen nötig werden, kommt ein Grund-Code dazu.
- Tests können eigene Command-Typen definieren (über `InternalsVisibleTo`), um die Pipeline unabhängig von Spiel-Features zu prüfen.
