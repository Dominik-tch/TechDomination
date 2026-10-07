# Projekt: Grand Strategy Game (Arbeitstitel)

Schnelleres Supremacy-1914-ähnliches Strategiespiel im Ersten Weltkrieg. Multiplayer (Host-basiert) + KI-Gegner, speicherbar.
Anforderungen: docs/requirements.md – vor neuen Features lesen.
Architektur: docs/architecture.md, Entscheidungen: docs/decisions/.

## Tech-Stack
- Godot 4.7 (.NET), C#, .NET 8
- Tests: xUnit
- Zielplattform MVP: Windows

## Projektstruktur
- `src/Game.Core/` – Simulation, KEINE Godot-Referenzen
- `src/Game.Core.Tests/` – Tests für Core
- `godot/` – Godot-Projekt, nur Darstellung, Input, Dateizugriff und Netzwerk-Anbindung
- `godot/data/` – Balancing- und Kartendaten (Ressourcen, Rezepte, Gebäude, Einheiten, Karte). Einzige Quelle für Daten.

## Architekturregeln (nicht verletzen)
- Game.Core kennt Godot nicht. Kein `using Godot` in Core.
- Jede Spieleraktion ist ein Command. Spieler, KI und Netzwerk erzeugen nur Commands.
- Der Spielzustand ist reine, serialisierbare Daten.
- Zeit wird in Ticks gemessen, nie in Sekunden oder Frames.
- Zufall nur über Seeded-RNGs: der Simulations-RNG im Spielzustand, die KI hat einen eigenen RNG mit festem Seed, dessen Zustand im Host-Spielstand liegt, nicht im GameState (siehe docs/decisions/0002).
- Simulation deterministisch halten: keine `float`/`double`, kein `System.Random`, keine Abhängigkeit von Dictionary-/HashSet-Reihenfolge.
- Balancing-Werte (Bauzeiten, Geschwindigkeiten, Kosten, Rezepte, Preise) stehen in `godot/data/`, nicht im Code.
- Game.Core kennt keine Dateipfade: Daten und Spielstände bekommt Core als Inhalt (String/Stream) übergeben.
- Nur der Host simuliert. Clients senden Commands und zeigen den empfangenen Zustand an (siehe docs/decisions/0001).
- Code plattformneutral halten: keine Windows-spezifischen Pfade oder APIs, Pfade über `user://` bzw. `Path.Combine`, Groß-/Kleinschreibung von Dateinamen exakt einhalten.

## Arbeitsweise
- Vor größeren Änderungen zuerst einen kurzen Plan vorschlagen und auf Freigabe warten.
- Kleine Schritte: ein Feature pro Aufgabe.
- Keine neuen NuGet-Pakete ohne Rückfrage.
- Features, die nicht in docs/requirements.md stehen oder dort unter „Nicht-Ziele“ fallen, nicht eigenmächtig einbauen, sondern vorschlagen.
- Bei Annahmen oder offenen Fragen aus docs/requirements.md nachfragen statt raten.
- Visuelles (UI-Layout, Farben) in .tscn-Dateien nur anlegen, nicht nachträglich umgestalten.
- Wenn eine Entscheidung von der Architektur abweicht: in docs/decisions/ dokumentieren.

## Befehle
- Build: `dotnet build`
- Tests: `dotnet test` – muss grün sein, bevor eine Aufgabe als fertig gilt
- Godot headless: `godot --headless --path godot/ ...`

## Code-Konventionen
- C#-Standardkonventionen, nullable aktiviert
- Jede neue Core-Funktion bekommt Tests
