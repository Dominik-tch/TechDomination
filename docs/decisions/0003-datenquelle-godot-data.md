# 0003 – `godot/data/` als einzige Datenquelle, Core ohne Pfade

- **Status:** angenommen
- **Datum:** 2026-10-08
- **Betrifft:** Projektstruktur, Datenladen, Tests ([architecture.md](../architecture.md), Abschnitte 2, 7)

## Kontext

Ursprünglich sollten Balancing-Daten in `data/` im Repository-Root liegen. Godot packt beim Export aber nur Dateien unter dem Projektordner `godot/` ein. Ein Kopierschritt von `data/` nach `godot/data/` hätte zwei Orte mit denselben Daten erzeugt.

## Entscheidung

- Alle Balancing- und Kartendaten liegen in **`godot/data/`**. Das ist die einzige Quelle; es gibt keinen Root-Ordner `data/`.
- **Game.Core kennt keine Dateipfade.** Core bekommt den Inhalt der Daten (Strings/Streams) übergeben und parst und validiert ihn.
- Godot liest die Dateien über `res://data/` und reicht den Inhalt an Core weiter. Dasselbe gilt für Spielstände unter `user://saves/`.
- Tests übergeben Core entweder eigene, kleine Test-Daten oder lesen die echten Dateien aus `godot/data/` (Pfad im Testprojekt über `Path.Combine` aufgebaut) und übergeben deren Inhalt.

## Begründung

- Keine doppelte Datenhaltung, kein Build-Schritt, der vergessen werden kann.
- Daten sind automatisch im Godot-Export enthalten.
- Core bleibt frei von Dateisystem- und Plattformdetails (Plattformneutralität) und ist ohne Dateien testbar.

## Konsequenzen

- CLAUDE.md wurde angepasst (Projektstruktur, Balancing-Regel, Regel „Core kennt keine Pfade“).
- Der Datenlader in Core hat eine Schnittstelle, die Inhalte entgegennimmt (z. B. Dateiname → Inhalt), keine Pfade.
- Ein Test validiert die echten Dateien aus `godot/data/`, damit fehlerhafte Daten früh auffallen.
