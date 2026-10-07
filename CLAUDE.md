# Projekt: Grand Strategy Game (Arbeitstitel)

Schnelleres Supremacy-1914-ähnliches Strategiespiel im Ersten Weltkrieg. Multiplayer (Host-basiert) + KI-Gegner, speicherbar.
Anforderungen: docs/requirements.md – vor neuen Features lesen.

## Tech-Stack
- Godot 4.7 (.NET), C#, .NET 8
- Tests: xUnit
- Zielplattform MVP: Windows

## Projektstruktur
- `src/Game.Core/` – Simulation, KEINE Godot-Referenzen
- `src/Game.Core.Tests/` – Tests für Core
- `godot/` – Godot-Projekt, nur Darstellung, Input und Netzwerk-Anbindung
- `data/` – Balancing-Daten (Ressourcen, Rezepte, Gebäude, Einheiten)

## Architekturregeln (nicht verletzen)
- Game.Core kennt Godot nicht. Kein `using Godot` in Core.
- Jede Spieleraktion ist ein Command. Spieler, KI und Netzwerk erzeugen nur Commands.
- Der Spielzustand ist reine, serialisierbare Daten.
- Zeit wird in Ticks gemessen, nie in Sekunden oder Frames.
- Zufall nur über den Seeded-RNG im Spielzustand.
- Balancing-Werte (Bauzeiten, Geschwindigkeiten, Kosten, Rezepte, Preise) stehen in `data/`, nicht im Code.
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
