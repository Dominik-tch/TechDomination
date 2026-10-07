# Requirements – Grand Strategy Game (Arbeitstitel)

## Vision

Ein Grand-Strategy-Spiel im Stil von Supremacy 1914, aber deutlich schneller: kürzere Bauzeiten, schnellere Truppenbewegung, eine Partie dauert Stunden statt Wochen. Man spielt gegen Freunde, gegen KI oder beides gemischt. Partien lassen sich wie bei Stellaris speichern und an einem anderen Abend gemeinsam fortsetzen. Die Wirtschaft ist bewusst tief: Rohstoffe werden über Produktionsketten zu fortgeschrittenen Gütern verarbeitet, die man für Militär und Ausbau braucht.

## Rahmen

| Punkt | Festlegung |
|---|---|
| Partiedauer | 3–6 Stunden, verteilt auf 2–3 Abende |
| Spieleranzahl | Variabel, abhängig von der Karte |
| Setting | Erster Weltkrieg |
| Karten | MVP: eine Europakarte. Später weitere |
| Plattform | MVP: nur Windows. Code bleibt plattformneutral |
| Zeitlimit | Beim Spielstart einstellbar |
| Bewegungstempo | Infanterie braucht bei Normalgeschwindigkeit im Schnitt 4–5 Minuten pro Provinz, abhängig von der Provinzgröße |

## MVP-Features

Jedes Feature gilt als fertig, wenn die unter „Fertig, wenn“ genannten Punkte erfüllt sind.

### 1. Karte und Provinzen
Europakarte mit Provinzen unterschiedlicher Größe. Jede Provinz hat einen Besitzer und produziert genau einen Basis-Rohstoff.
**Fertig, wenn:** Karte wird angezeigt, Provinzen sind anklickbar, Besitzer ist farblich erkennbar, Provinzinfo (Rohstoff, Gebäude) ist sichtbar.

### 2. Zeit und Geschwindigkeit
Die Simulation läuft in Ticks. Es gibt mehrere Geschwindigkeitsstufen.
- Der Host bestimmt die Geschwindigkeit.
- Jeder Spieler darf pausieren.

**Fertig, wenn:** Pause und Geschwindigkeitswechsel wirken für alle Spieler gleichzeitig.

### 3. Wirtschaft
- Ca. 25 Ressourcen in zwei Stufen:
  - **Basis-Rohstoffe** (z. B. Holz, Stahl, Getreide, Fisch, Gas, Kohle, Öl), jede Provinz produziert einen.
  - **Fortgeschrittene Güter** (z. B. Schienen, Motoren, Flügel, Sättel), herstellbar in spezifischen Fabriken nach Rezept (z. B. 2 Stahl + 5 Holz → 1 Schiene) oder kaufbar.
- Alle Ressourcen liegen in einem **nationalen Pool** und sind überall sofort verfügbar.
- **Geld** kommt aus Steuern pro Provinz und aus dem Verkauf von Ressourcen.
- **Markt:** Jeder Spieler hat einen eigenen Markt, der nur von seinen eigenen Käufen und Verkäufen abhängt. Kaufen erhöht den Preis der Ressource, der Preis sinkt danach langsam zum Basispreis zurück.
- Konkrete Ressourcen, Rezepte und Preise stehen in separaten Daten-/Konfigurationsdateien, nicht in diesem Dokument.

**Fertig, wenn:** Provinzen produzieren laufend, Fabriken verarbeiten nach Rezept, Kaufen/Verkaufen funktioniert inkl. Preisanstieg und Preiserholung.

### 4. Gebäude
Gebäude können in jeder Provinz gebaut werden und haben Ausbaustufen. Bauen kostet Ressourcen und Zeit.
**Fertig, wenn:** Bau, Bauzeit, Ausbau und Wirkung der Gebäude funktionieren.

### 5. Militär (nur Land)
- Armeen sind **gemischte Stacks** aus mehreren Einheitentypen.
- Bewegung entlang benachbarter Provinzen, Dauer abhängig von Provinzgröße und Einheitentyp.
- **Kampf** läuft automatisch über Zeit, in Runden alle X Ticks, sobald feindliche Armeen in derselben Provinz sind.
- Provinzen werden durch Einmarsch erobert.

**Fertig, wenn:** Armeen können aufgestellt, zusammengelegt/geteilt, bewegt werden, Kämpfe werden ausgetragen, Provinzen wechseln den Besitzer.

### 6. Diplomatie
Nur Krieg und Frieden zwischen Nationen.
**Fertig, wenn:** Kriegserklärung und Friedensschluss funktionieren und Kämpfe nur im Kriegszustand stattfinden.

### 7. Sieg
- **Sofortsieg:** Wer mehr als 60 % der Karte kontrolliert, gewinnt.
- **Sonst:** Bei Ablauf des Zeitlimits gewinnt der höchste Punktestand.
- **Punkte** gibt es für Provinzen und Gebäude (Infrastruktur).

**Fertig, wenn:** Punktestand ist laufend sichtbar, beide Siegbedingungen beenden die Partie korrekt.

### 8. Multiplayer
- Ein Spieler hostet, die Simulation läuft beim Host.
- Verbindung per IP, auch über virtuelle LANs wie Hamachi.
- **Verbindungsverlust:** Das Spiel pausiert, ein Dialog zeigt „Warte auf Spieler X…“. Nur der Host kann den Spieler durch die KI ersetzen und weiterspielen.
- **Rückkehr:** Ein zurückkehrender Spieler übernimmt seine Nation wieder von der KI.

**Fertig, wenn:** 2+ Spieler können eine Partie zusammen spielen, Disconnect und Rejoin funktionieren wie beschrieben.

### 9. KI
- Eine Schwierigkeitsstufe.
- Steuert Nationen ohne menschlichen Spieler, auch gemischt mit Menschen in einer Partie.
- Nutzt dieselben Aktionen wie menschliche Spieler.

**Fertig, wenn:** Die KI baut Wirtschaft und Militär auf, führt Krieg und ist für einen Anfänger ein ernstzunehmender Gegner.

### 10. Speichern und Laden
- Der Host speichert den kompletten Spielstand.
- Beim Laden verbinden sich die Spieler und übernehmen ihre Nationen. Fehlende Spieler werden wie bei einem Verbindungsverlust behandelt.

**Fertig, wenn:** Eine Multiplayer-Partie kann gespeichert, das Spiel beendet und die Partie später mit identischem Zustand fortgesetzt werden.

## Später (nach dem MVP)

- Luft- und Seeeinheiten
- Forschung (schaltet Einheiten und Gebäude frei)
- Nebel des Krieges
- Globaler Markt zwischen Spielern
- Weitere Karten
- Mehrere KI-Schwierigkeitsstufen
- Linux- und macOS-Builds

## Nicht-Ziele

Diese Punkte werden bewusst **nicht** gebaut, solange sie nicht hier ergänzt werden:

- Kein Browser- oder Mobile-Build
- Keine Matchmaking-Server, Accounts oder Online-Lobbys
- Keine manuelle Schlachtsteuerung / Echtzeit-Taktik
- Kein Ressourcentransport zwischen Provinzen (nationaler Pool)
- Keine Bündnisse oder weitere Diplomatie über Krieg/Frieden hinaus
- Kein direkter Handel zwischen Spielern im MVP

## Annahmen (zu bestätigen)

- Verkaufen senkt den Marktpreis, analog zum Kaufen.
- Einheiten kosten Ressourcen aus der Produktionskette (z. B. Kavallerie braucht Sättel).
- Die 60 % beim Sofortsieg beziehen sich auf die Anzahl der Provinzen.

## Offene Fragen

- Wer darf eine Pause wieder aufheben?
- Welche Landeinheiten gibt es im MVP?
- Wovon hängen Steuern ab (pauschal pro Provinz, Gebäude, Bevölkerung)?
- Gibt es Bevölkerung oder Moral?
- Wie viele Provinzen hat die Europakarte?
- Startbedingungen: Nationenwahl, Startressourcen, Startarmeen?
- Was passiert mit Truppen in fremdem Gebiet bei Friedensschluss?
