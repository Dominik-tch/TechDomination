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
- Jeder Spieler darf eine Pause wieder aufheben.

**Fertig, wenn:** Pause und Geschwindigkeitswechsel wirken für alle Spieler gleichzeitig.

### 3. Wirtschaft
- Ca. 25 Ressourcen in zwei Stufen:
  - **Basis-Rohstoffe** (z. B. Holz, Stahl, Getreide, Fisch, Gas, Kohle, Öl), jede Provinz produziert genau einen. Der Rohstoff einer Provinz wird auf der Karte durch ein Symbol dargestellt (z. B. Holzstamm, Ölfass).
  - **Fortgeschrittene Güter** (z. B. Schienen, Motoren, Flügel, Sättel), herstellbar in spezifischen Fabriken nach Rezept (z. B. 2 Stahl + 5 Holz → 1 Schiene) oder kaufbar.
- Alle Ressourcen liegen in einem **nationalen Pool** und sind überall sofort verfügbar.
- **Produktion:** feste Menge je Rohstoff, unabhängig von der Provinzgröße. Bestimmte Gebäude können später den Rohstoff-Output erhöhen.
- **Geld** kommt aus Steuern pro Provinz und aus dem Verkauf von Ressourcen. Steuern sind ein fester Betrag pro Provinz, unabhängig von der Provinzgröße; Gebäude erhöhen die Steuern nicht.
- **Start:** Jede Nation beginnt mit einem festen Startbetrag an Geld und an Rohstoffen (Werte in den Daten).
- **Markt:** Jeder Spieler hat einen eigenen Markt, der nur von seinen eigenen Käufen und Verkäufen abhängt. Kaufen erhöht den Preis der Ressource, Verkaufen senkt ihn; danach bewegt sich der Preis langsam zum Basispreis zurück. Verkaufen bringt 80 % des aktuellen Preises.
- **Fabriken** sind Gebäude mit einem festen Rezept und bis zu 5 Stufen. Jede Stufe zählt wie eine eigene Fabrik (ein Rezept-Durchlauf je Fabrikzyklus). Rezepte werden in ganzen Mengen angezeigt (z. B. 2 Öl → 1 Treibstoff); wie oft eine Fabrik läuft, steht in den Daten. Ausbaustufen kosten je 50 % mehr als der Bau der Fabrik.
- **Fabrik-Steuerung:** Eine Verwaltungsansicht zeigt alle Fabriktypen mit je einem Schieberegler, der festlegt, wie viele Fabriken dieses Typs laufen (so viele Schritte, wie es Fabriken bzw. Stufen gibt; z. B. bei 2 Fabriken: 0, 1 oder 2). Neue Fabriken laufen automatisch mit, solange der Regler nicht heruntergezogen wurde.
- Konkrete Ressourcen, Rezepte und Preise stehen in separaten Daten-/Konfigurationsdateien, nicht in diesem Dokument.

**Fertig, wenn:** Provinzen produzieren laufend, Fabriken verarbeiten nach Rezept, Kaufen/Verkaufen funktioniert inkl. Preisanstieg und Preiserholung.

### 4. Gebäude
Gebäude können in jeder Provinz gebaut werden und haben Ausbaustufen. Bauen kostet Ressourcen und Zeit.
- **Förderanlage:** erhöht den Rohstoff-Output der Provinz um 10 % des Basiswerts je Stufe, maximal 5 Stufen (Stufe 5 = +50 %).
- Weitere Gebäude kommen mit ihren Features (Fabriken mit der Wirtschaft, Kaserne und Festung mit dem Militär).
- Pro Provinz gibt es höchstens **eine Baustelle** gleichzeitig.
- Die Kosten werden beim Baustart bezahlt. Ein Bau kann abgebrochen werden, die Kosten werden dann **voll erstattet**.
- Bei Eroberung gehen die Gebäude an den neuen Besitzer über; eine laufende Baustelle verfällt ohne Erstattung.
- Fertige Bauten und abgelehnte Aktionen werden als kurze Benachrichtigung angezeigt.
**Fertig, wenn:** Bau, Bauzeit, Ausbau und Wirkung der Gebäude funktionieren.

### 5. Militär (nur Land)
- Armeen sind **gemischte Stacks** aus mehreren Einheitentypen.
- Bewegung entlang benachbarter Provinzen, Dauer abhängig von Provinzgröße und Einheitentyp.
- **Kampf** läuft automatisch über Zeit, in Runden alle X Ticks, sobald feindliche Armeen in derselben Provinz sind.
- **Laufender Verbrauch:** Armeen verbrauchen Konserven als Unterhalt, solange sie existieren; motorisierte Einheiten verbrauchen Treibstoff; Munition wird im Kampf verbraucht. Kriege kosten dadurch laufend Wirtschaftsleistung. Mengen und Takt werden mit M7/M8 festgelegt.
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
- Speichern und Laden über ein Menü (Esc). Nach dem Laden ist das Spiel pausiert.
- Spielstände einer anderen Spielversion oder mit anderem Regelwerk (geänderte Daten) werden mit einer Meldung abgelehnt.
- **Automatisches Speichern** alle 10 Minuten Spielzeit (bei Standardgeschwindigkeit), rotierend in 3 Dateien.

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
- Keine Bevölkerung und keine Moral im MVP

## Annahmen (zu bestätigen)

- Einheiten kosten Ressourcen aus der Produktionskette (z. B. Kavallerie braucht Sättel).
- Die 60 % beim Sofortsieg beziehen sich auf die Anzahl der Provinzen.

## Offene Fragen

- Welche Landeinheiten gibt es im MVP?
- Wie viele Provinzen hat die Europakarte?
- Startbedingungen: Nationenwahl, Startarmeen?
- Was passiert mit Truppen in fremdem Gebiet bei Friedensschluss?
