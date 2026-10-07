# 0002 – Eigener Zufallsgenerator für die KI

- **Status:** angenommen
- **Datum:** 2026-10-08
- **Betrifft:** KI, Speichern, Determinismus ([architecture.md](../architecture.md), Abschnitte 8, 10, 11)

## Kontext

Die Architekturregel lautete „Zufall nur über den Seeded-RNG im Spielzustand“. Die KI braucht ebenfalls Zufall (z. B. um nicht vorhersehbar zu sein). Würde sie den Simulations-RNG mitbenutzen, hinge jedes Kampfergebnis davon ab, wie oft und wann die KI „nachgedacht“ hat. Eine Änderung an der KI würde dann Simulationsergebnisse verschieben, und Tests der Simulation wären nicht mehr unabhängig von der KI.

## Entscheidung

Die KI bekommt einen **eigenen Seeded-RNG mit festem Seed** (abgeleitet aus dem Partie-Seed und der Nation-ID).

- Der Simulations-RNG bleibt ausschließlich der Simulation vorbehalten.
- Der KI-Zustand inklusive KI-RNG gehört zum **Host-only-Teil** des Spielstands und wird gespeichert, aber nicht an Clients gesendet.
- Beide RNGs nutzen dieselbe eigene PRNG-Implementierung (nicht `System.Random`).

## Begründung

- **Reproduzierbare Tests:** KI-gegen-KI-Partien laufen bei gleichem Seed identisch ab und lassen sich als Regressionstests verwenden.
- **Entkopplung:** Änderungen am KI-Verhalten verändern nicht die Zufallsfolge der Simulation.
- **Speichern/Laden:** Weil der KI-RNG im Spielstand steht, verhält sich die KI nach dem Laden genauso wie ohne Unterbrechung – Voraussetzung für den Speicher-Hash-Test mit KI.

## Konsequenzen

- Die Regel in CLAUDE.md wurde präzisiert: Zufall nur über Seeded-RNGs (Simulation im Spielzustand, KI mit eigenem RNG).
- `SaveGame` hat einen Host-only-Bereich für KI-Zustand.
- Wird eine Nation durch die KI ersetzt oder zurückgegeben, bleibt ihr KI-RNG erhalten und läuft weiter.
