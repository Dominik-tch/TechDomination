# 0002 – Eigener Zufallsgenerator für die KI

- **Status:** angenommen
- **Datum:** 2026-10-08
- **Betrifft:** KI, Speichern, Determinismus ([architecture.md](../architecture.md), Abschnitte 8, 10, 11)

## Kontext

Die Architekturregel lautete „Zufall nur über den Seeded-RNG im Spielzustand“. Die KI braucht ebenfalls Zufall (z. B. um nicht vorhersehbar zu sein). Würde sie den Simulations-RNG mitbenutzen, hinge jedes Kampfergebnis davon ab, wie oft und wann die KI „nachgedacht“ hat. Eine Änderung an der KI würde dann Simulationsergebnisse verschieben, und Tests der Simulation wären nicht mehr unabhängig von der KI.

## Entscheidung

Die KI bekommt einen **eigenen Seeded-RNG mit festem Seed** (abgeleitet aus dem Partie-Seed und der Nation-ID).

- Der Simulations-RNG bleibt ausschließlich der Simulation vorbehalten.
- Der Zustand des KI-RNG (und der übrige KI-Zustand) ist **nicht Teil des synchronisierten `GameState`**. Er wird im **Host-only-Teil des Spielstands** mitgespeichert, beim Laden wiederhergestellt und nie an Clients gesendet.
- Beide RNGs nutzen dieselbe eigene PRNG-Implementierung (nicht `System.Random`).

## Begründung

- **Reproduzierbare Tests:** KI-gegen-KI-Partien laufen bei gleichem Seed identisch ab und lassen sich als Regressionstests verwenden.
- **Entkopplung:** Änderungen am KI-Verhalten verändern nicht die Zufallsfolge der Simulation.
- **Speicher-Hash-Test mit KI:** Der Test „N Ticks → speichern → laden → M Ticks ≡ N+M Ticks“ hält nur, wenn die KI nach dem Laden exakt dieselben Commands erzeugt wie ohne Unterbrechung. Dafür muss der KI-RNG-Zustand mitgespeichert werden. Würde er beim Laden neu aus dem Seed erzeugt, wiche die KI ab und mit ihr der `GameState`.
- **Nicht im `GameState`:** Clients brauchen den KI-RNG nicht, und er soll nicht mit jedem Snapshot übertragen werden.

## Konsequenzen

- Die Regel in CLAUDE.md wurde präzisiert: Zufall nur über Seeded-RNGs (Simulation im Spielzustand, KI mit eigenem RNG).
- `SaveGame` hat einen Host-only-Bereich für den KI-Zustand inklusive KI-RNG-Zustand, getrennt vom `GameState`.
- Der Speicher-Hash-Test vergleicht ab M10 neben dem `GameState`-Hash auch den KI-Zustand.
- Wird eine Nation durch die KI ersetzt oder zurückgegeben, bleibt ihr KI-RNG erhalten und läuft weiter.
