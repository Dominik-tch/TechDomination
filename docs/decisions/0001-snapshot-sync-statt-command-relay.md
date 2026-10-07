# 0001 – Snapshot-Sync statt Command-Relay

- **Status:** angenommen
- **Datum:** 2026-10-08
- **Betrifft:** Netzwerk, Session, Godot-Darstellung ([architecture.md](../architecture.md), Abschnitt 9)

## Kontext

Der Multiplayer ist host-basiert (Feature 8). Zur Synchronisation der Clients standen zwei Modelle zur Wahl:

1. **Command-Relay:** Der Host verteilt pro Tick die gesammelten Commands, alle Rechner simulieren deterministisch mit. Regelmäßige Hash-Prüfung, Snapshot bei Abweichung oder Beitritt.
2. **Snapshot-Sync:** Nur der Host simuliert und schickt den Clients regelmäßig den Zustand.

## Entscheidung

**Snapshot-Sync.**

- Nur der Host simuliert und führt die KI aus. Clients senden Commands an den Host und zeigen den empfangenen Zustand an. Armeebewegungen werden zwischen zwei Updates interpoliert.
- Der Host sendet Zustandsupdates mit fester, konfigurierbarer Rate (z. B. 2–5 pro Sekunde), inklusive Session-Status (Pause, Tempo, Warten auf Spieler).
- Start mit vollständigen, komprimierten Snapshots. Die Größe wird gemessen; Deltas werden erst eingeführt, wenn die Messung es verlangt.
- Keine Hash-Prüfung zwischen Host und Clients.

## Begründung

- **Robustheit:** Ein Client kann nicht vom Host abweichen („desyncen“), weil er den Zustand nur übernimmt. Ein Determinismus-Fehler ist damit kein Multiplayer-Absturz mehr, sondern höchstens ein Testfehler.
- **Einfachheit:** Kein Abgleich von Tick-Batches, keine Hash-Prüfung, keine Resync-Logik. Beitritt, Rückkehr nach Disconnect und laufendes Spiel nutzen denselben Weg – jedes Update ist ein vollständiger Snapshot.
- **Wiederverwendung:** Snapshot und Spielstand teilen die `GameState`-Serialisierung.
- **Erweiterbarkeit:** Nebel des Krieges (nach dem MVP) lässt sich durch Filtern des Snapshots pro Empfänger umsetzen; beim Command-Relay hätte jeder Client den vollständigen Zustand.
- **Bandbreite:** Ein Strategiespiel mit wenigen hundert Provinzen und Armeen erzeugt voraussichtlich kleine Snapshots; bei 2–5 Updates pro Sekunde und Kompression sollte das auch über Hamachi reichen. Das wird gemessen statt angenommen.

## Konsequenzen

- Clients zeigen den Zustand mit bis zu einem Update-Intervall Verzögerung; Bewegungen werden interpoliert, damit sie flüssig wirken.
- Commands vom Client haben eine Latenz von bis zu einem Tick plus einem Update-Intervall. Die UI kann ausstehende Commands markieren.
- Events (Benachrichtigungen) werden im Update mitgeschickt, weil Clients `Step()` nicht ausführen.
- Views referenzieren Zustandsobjekte nur über IDs, da der Client-Zustand bei jedem Update ersetzt wird.
- Die Determinismus-Regeln bleiben trotzdem bestehen – für reproduzierbare Tests, den Speicher-Hash-Test und das Nachspielen von Fehlern über Command-Logs.
- Falls die Bandbreite nicht reicht: zuerst Update-Rate senken oder Kompression wechseln, dann Deltas einführen.
