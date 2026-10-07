# Architektur – Grand Strategy Game (Arbeitstitel)

Grundlage: [CLAUDE.md](../CLAUDE.md) (Architekturregeln) und [requirements.md](requirements.md) (Features).
Getroffene Entscheidungen mit Begründung: [decisions/](decisions/).

---

## 1. Überblick

```
┌──────────────────────────── HOST (godot/) ─────────────────────────────┐      ┌───────────── CLIENT (godot/) ─────────────┐
│                                                                        │      │                                           │
│  MapView / UI ──liest──► GameState (live, read-only)                   │      │  MapView / UI ──liest──► GameState        │
│      │                        ▲                                        │      │      │                  (letzter Snapshot)│
│      │ Commands               │ Step()                                 │      │      │ Commands              ▲            │
│      ▼                        │                                        │      │      ▼                       │            │
│  ICommandSink ──► Session ──► SimulationDriver (Echtzeit → Ticks)      │      │  ICommandSink ──┐   ClientStateMirror     │
│                     ▲   │                                              │      │                 │            ▲            │
│                     │   └──► StateBroadcaster (2–5 Updates/s) ─────────┼──────┼─────────────────┼────────────┘            │
│                     └──────────────────────────────── Commands ◄───────┼──────┼─────────────────┘                         │
└────────────────────────────────────────────────────────────────────────┘      └───────────────────────────────────────────┘
                      │ ProjectReference                                                     │ ProjectReference
┌──────────────────────────────── src/Game.Core/ (reines C#, kein Godot, keine Pfade) ───────────────────────────────────────┐
│  Data (Regelwerk)   State (GameState)   Commands   Systems (Tick)   AI   Session   Save   Protocol (Nachrichten)           │
└────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────┘
```

**Leitideen:**

- Die Simulation ist eine **deterministische Funktion**
  `(Zustand bei Tick N, Commands für Tick N) → Zustand bei Tick N+1`.
  Das macht Tests, Speichern/Laden und Fehlersuche reproduzierbar.
- **Nur der Host simuliert.** Clients senden Commands und zeigen den Zustand an, den der Host ihnen schickt ([Entscheidung 0001](decisions/0001-snapshot-sync-statt-command-relay.md)).

---

## 2. Projektaufbau

| Projekt / Ordner | Inhalt | Referenziert |
|---|---|---|
| `src/Game.Core` | Simulation, Regelwerk, KI, Speicherformat, Netzwerkprotokoll (nur Nachrichten, kein Transport) | – |
| `src/Game.Core.Tests` | xUnit-Tests für Core | Game.Core |
| `godot/` | Godot-.NET-Projekt: Szenen, Darstellung, Input, Dateizugriff, ENet-Transport | Game.Core |
| `godot/data/` | JSON-Dateien mit Balancing und Karte – **einzige Datenquelle** ([Entscheidung 0003](decisions/0003-datenquelle-godot-data.md)) | – |

- Eine Solution im Root enthält alle drei C#-Projekte, damit `dotnet build` / `dotnet test` alles abdeckt.
- Serialisierung über `System.Text.Json`, Kompression über `System.IO.Compression` (beides in .NET 8 enthalten, keine neuen NuGet-Pakete).
- Netzwerk über Godots eingebautes `ENetMultiplayerPeer` (kein NuGet-Paket).

### Interne Struktur von Game.Core

| Ordner / Namespace | Verantwortung |
|---|---|
| `Data` | Unveränderliches Regelwerk (`GameData`): Ressourcen, Rezepte, Gebäude, Einheiten, Karte (Provinzen, Nachbarschaften, Größen), Geschwindigkeitsstufen, Intervalle. Wird aus übergebenem Inhalt geparst und validiert. |
| `State` | Veränderlicher, serialisierbarer Spielzustand (`GameState`). Nur Daten, keine Logik. |
| `Commands` | Command-Typen, Validierung, Anwendung. |
| `Systems` | Tick-Systeme (Produktion, Bau, Markt, Bewegung, Kampf, Sieg …). |
| `Simulation` | `Simulation.Step()` – orchestriert Commands und Systeme pro Tick. |
| `Events` | Ereignisse pro Tick (für UI-Benachrichtigungen, nicht Teil des Zustands). |
| `AI` | KI-Spieler: liest Zustand, erzeugt Commands. |
| `Session` | Pause, Geschwindigkeit, Spieler↔Nation-Zuordnung, Verbindungsstatus. |
| `Save` | Speicherformat, Versionierung, (De-)Serialisierung über `Stream`. |
| `Protocol` | Netzwerknachrichten, Snapshot-Kodierung und -Kompression. |
| `Determinism` | Eigener Seeded-RNG, Zustands-Hash für Tests. |

---

## 3. Spielzustand

### 3.1 Trennung Regelwerk ↔ Zustand

- **`GameData` (statisch):** alles aus `godot/data/`. Wird nie verändert, nicht gespeichert. Im Spielstand steht nur eine **Kennung + Hash** des Regelwerks, damit Laden mit abweichenden Daten erkannt wird.
- **`GameState` (dynamisch):** alles, was sich während der Partie ändert.

### 3.2 Inhalt von `GameState`

| Bereich | Inhalt |
|---|---|
| Kopf | aktueller Tick, Simulations-RNG-Zustand, Partie-Einstellungen (Zeitlimit in Ticks, Siegschwelle), Partiestatus (läuft / beendet + Sieger), nächste freie IDs |
| Nationen | ID, Name, Farbe-Schlüssel, nationaler Ressourcenpool, Geld, Punktestand |
| Provinzen | ID (verweist auf Kartendaten), Besitzer, Gebäude mit Stufe, Bauaufträge mit Restticks, Fabrikproduktion |
| Märkte | pro Nation: aktueller Preis je Ressource |
| Armeen | ID, Besitzer, Provinz, Einheiten (Typ → Anzahl/Stärke), Bewegungsauftrag (Pfad, Fortschritt in Ticks) |
| Kämpfe | laufende Kämpfe je Provinz, nächster Runden-Tick |
| Diplomatie | Menge der Nationenpaare im Krieg |

Der Inhalt wächst mit den Meilensteinen; die Tabelle ist der Zielstand für das MVP.

### 3.3 Regeln für den Zustand

- **IDs:** typisierte Ganzzahl-IDs (`ProvinceId`, `NationId`, `ArmyId`), vergeben über Zähler im Zustand.
- **Sammlungen:** Listen sortiert nach ID bzw. `SortedDictionary`. Keine Logik, die von der Iterationsreihenfolge eines `Dictionary`/`HashSet` abhängt.
- **Zahlen:** keine `float`/`double` in der Simulation. Mengen und Preise als Ganzzahlen in festen Untereinheiten (z. B. Ressourcen in 1/1000, Geld in Cent).
- **Zufall:** eigener kleiner PRNG (z. B. PCG/xorshift) mit Zustand im `GameState`. Nicht `System.Random`, da dessen Zustand nicht serialisierbar und dessen Algorithmus nicht garantiert stabil ist.
- **Schreibschutz für Godot:** Zustandsklassen haben öffentliche Getter und `internal` Setter. Godot (andere Assembly) kann damit nur lesen; der Compiler erzwingt, dass Änderungen nur über Commands laufen. Tests bekommen `InternalsVisibleTo`.

---

## 4. Tick-System

### 4.1 Ablauf eines Ticks

`Simulation.Step(state, data, commandsForThisTick) → TickEvents`

1. **Commands anwenden** – in fester Reihenfolge (nach Nation-ID, dann Sequenznummer). Ungültige Commands werden verworfen und als Event gemeldet.
2. **Systeme ausführen** – feste Reihenfolge:
   1. Produktion (Basis-Rohstoffe in den Pool)
   2. Fabriken (Rezepte verarbeiten)
   3. Bau (Bauaufträge fortschreiten / abschließen)
   4. Steuern (Geld)
   5. Markt (Preise erholen sich Richtung Basispreis)
   6. Bewegung (Armeen rücken vor, betreten Provinzen)
   7. Kampf (alle X Ticks eine Runde, nur bei Krieg)
   8. Eroberung (Besitzerwechsel)
   9. Punkte & Sieg (Sofortsieg, Zeitlimit)
3. **Tick erhöhen.**

- Jedes System ist eine zustandslose Klasse mit `Update(state, data, ctx)`.
- Systeme mit Intervall (z. B. Steuern alle N Ticks, Kampfrunde alle X Ticks) lesen das Intervall aus `GameData`.
- Rückgabe: Liste von **Events** (z. B. `BuildingCompleted`, `ProvinceConquered`, `BattleStarted`, `CommandRejected`). Events sind flüchtig und dienen nur UI/Benachrichtigungen.

### 4.2 Echtzeit → Ticks (außerhalb von Core)

- Core kennt keine Sekunden. Der `SimulationDriver` (Godot, **nur beim Host** bzw. im Einzelspieler) akkumuliert `delta` und ruft `Step()` so oft auf, wie es die Geschwindigkeitsstufe verlangt.
- Die Zuordnung „Stufe → Ticks pro Sekunde“ steht in `godot/data/` (Balancing). Daraus ergibt sich z. B. die geforderte Infanterie-Bewegung von 4–5 Minuten pro Provinz.
- Pause / Geschwindigkeit gehören zur **Session**, nicht zum Simulationszustand (siehe 6).
- Die Simulation läuft im MVP auf dem Hauptthread.

---

## 5. Command-System

### 5.1 Aufbau

- **Command:** reiner, serialisierbarer Datensatz (Record), z. B. `BuildBuilding(provinceId, buildingType)`.
- **Envelope:** `CommandEnvelope { ExecuteAtTick, Issuer (NationId), Sequence, Command }`. Der **Issuer wird vom Host gesetzt** (aus der Verbindung), nie vom Client übernommen.
- **Handler:** pro Command-Typ ein Handler mit `Validate(state, data, issuer)` und `Apply(state, data, issuer)`. `Apply` setzt erfolgreiche Validierung voraus und wird nur innerhalb von `Step()` aufgerufen.
- Polymorphe Serialisierung über `System.Text.Json`-Typdiskriminatoren.
- **Command-Log:** Der Host kann alle ausgeführten Envelopes protokollieren. Zusammen mit einem Spielstand lässt sich damit jede Partie für Tests und Fehlersuche exakt nachspielen.

### 5.2 Simulations-Commands (MVP, vorläufig)

| Bereich | Commands |
|---|---|
| Gebäude | Bauen, Ausbauen, (Bau abbrechen?) |
| Wirtschaft | Ressource kaufen, Ressource verkaufen, Fabrikproduktion einstellen |
| Militär | Einheiten rekrutieren, Armee bewegen, Armeen zusammenlegen, Armee teilen, Bewegung abbrechen |
| Diplomatie | Krieg erklären, Frieden anbieten / annehmen |

### 5.3 Session-Commands (nicht Teil der Simulation)

Pause, Fortsetzen, Geschwindigkeit setzen (nur Host), Nation durch KI ersetzen (nur Host), Nation zurückübernehmen. Diese ändern nicht `GameState`, sondern steuern, **ob und wie schnell** Ticks laufen bzw. wer Commands für eine Nation erzeugt.

### 5.4 Wer erzeugt Commands?

| Quelle | Weg |
|---|---|
| Lokaler Spieler am Host (UI) | `ICommandSink.Submit(cmd)` → Session |
| Netzwerk-Client | Client sendet an Host → Session |
| KI | `AiPlayer.Think(state, data)` → Session, läuft nur beim Host |

Alle landen in derselben **Command-Queue beim Host**, der ihnen den Ausführungstick zuweist (nächster Tick).

---

## 6. Session-Schicht

`Session` (in Core, reines C#, testbar) verwaltet:

- Spieler-Slots: Nation ↔ Spieler (Name) oder KI
- Verbindungsstatus pro Spieler, „Warte auf Spieler X“
- Pausenzustand (wer hat pausiert) und Geschwindigkeitsstufe
- Command-Queue und Zuweisung des Ausführungsticks

Der **Session-Status** (Pause, Tempo, wartet auf Spieler) ist ein eigener serialisierbarer Datensatz, der mit jedem Zustandsupdate an die Clients geht.

Godot steuert nur Zeit und Transport bei; die Regeln („wer darf was“) liegen testbar in Core.

---

## 7. Wie Godot auf Core zugreift

- **Zustandsquelle:** Views lesen über eine gemeinsame Schnittstelle (`IGameStateSource`: aktueller `GameState`, Session-Status, neue Events). Beim Host liefert sie den Live-Zustand, beim Client den zuletzt empfangenen Snapshot. Die Views wissen nicht, ob sie auf Host oder Client laufen.
- **Nur über IDs referenzieren:** Beim Client wird der Zustand bei jedem Update komplett ersetzt. Views halten deshalb IDs (`ProvinceId`, `ArmyId`), keine Objektreferenzen auf Zustandsobjekte.
- **Schreiben:** ausschließlich über `ICommandSink`. Host/Einzelspieler: Sink ist die lokale Session. Client: Sink sendet ans Netzwerk.
- **Events:** Beim Host direkt aus `Step()`; beim Client im Zustandsupdate mitgeliefert (siehe 9.2).
- **Interpolation:** Armeebewegung ist in Ticks gespeichert (Fortschritt/Gesamtdauer). Godot interpoliert die Position visuell – beim Host zwischen Ticks, beim Client zwischen zwei Updates anhand von Tick, Tempo und Bewegungsfortschritt.
- **Karte:** Provinzformen und -positionen kommen aus Kartendaten. Klick → Provinz-ID (ID-Farbbild oder Polygone, Festlegung in Meilenstein 1).
- **Dateizugriff:** Godot liest die Dateien aus `res://data/` (= `godot/data/`) und Spielstände aus `user://saves/` und übergibt Core den **Inhalt** (String/Stream). Core kennt keine Pfade ([Entscheidung 0003](decisions/0003-datenquelle-godot-data.md)).

---

## 8. Speichern und Laden

- **Spielstand = `SaveGame`:**
  - Formatversion
  - Regelwerk-Kennung + Hash
  - `GameState` (vollständig, inkl. Simulations-RNG und Tick)
  - Session-Metadaten: Spieler-Slots (Nation → Spielername / KI), damit Spieler beim Laden ihre Nation wieder übernehmen
  - Host-only-Zustand: KI-Zustand inkl. KI-RNG ([Entscheidung 0002](decisions/0002-eigener-ki-zufallsgenerator.md))
- Format: JSON über `System.Text.Json`, komprimiert. Dieselbe `GameState`-Serialisierung wird für Netzwerk-Snapshots verwendet.
- Gespeichert wird **nur an Tick-Grenzen**, nur vom Host.
- Ablage: `user://saves/` (Godot), Core arbeitet nur mit Streams.
- **Laden im Multiplayer:** Host lädt, Spieler verbinden sich und werden über den Spielernamen ihrer Nation zugeordnet. Nicht verbundene Spieler → Status „Verbindungsverlust“ (pausiert, Host kann KI einsetzen).

**Kerntest (ab Meilenstein 0, wächst mit jedem Feature):**
`N Ticks laufen → speichern → laden → M Ticks` ergibt denselben Zustands-Hash wie `N+M Ticks am Stück` mit denselben Commands (inkl. KI).

---

## 9. Netzwerk

### 9.1 Modell: Host-autoritativ mit Snapshot-Sync

Entscheidung und Begründung: [0001 – Snapshot-Sync statt Command-Relay](decisions/0001-snapshot-sync-statt-command-relay.md).

- **Nur der Host simuliert** und führt die KI aus.
- **Clients** senden Commands an den Host, empfangen Zustandsupdates und zeigen sie an. Clients rufen `Step()` nie auf.
- Armeebewegungen werden auf dem Client zwischen zwei Updates **interpoliert** (siehe 7).
- Eine Hash-Prüfung zwischen Host und Clients gibt es nicht – der Client übernimmt den Zustand des Hosts unverändert.

### 9.2 Nachrichten (`Game.Core/Protocol`)

| Richtung | Nachricht | Inhalt |
|---|---|---|
| Client → Host | `Hello` | Spielversion, Regelwerk-Hash, Spielername |
| Host → Client | `Welcome` / `Rejected` | zugewiesene Nation bzw. Ablehnungsgrund |
| Client → Host | `SubmitCommand` | Command + Client-Sequenznummer |
| Client → Host | `SessionRequest` | Pause, Fortsetzen, Tempo (nur Host), Nation zurückübernehmen |
| Host → Client | `StateUpdate` | Tick, vollständiger komprimierter `GameState`, Session-Status, Events seit dem letzten Update |
| Host → Client | `CommandRejected` | Client-Sequenznummer + Grund |

- Der Snapshot enthält **keinen Host-only-Zustand** (KI-Zustand, KI-RNG).
- Events werden für den Empfänger gefiltert (nur Events, die seine Nation betreffen, plus globale wie Kriegserklärungen).

### 9.3 Update-Rate

- Der Host sendet `StateUpdate` mit **fester, konfigurierbarer Rate** (Standard aus `godot/data/`, z. B. 2–5 pro Sekunde), unabhängig von der Tick-Rate.
- Ändert sich der Session-Status (Pause, Tempo, Warten auf Spieler), sendet der Host **sofort** ein Update, damit Pause und Tempowechsel ohne Verzögerung bei allen ankommen.
- Während einer Pause werden nur Updates bei Statusänderungen gesendet.
- Latenz eines Commands: bis zum nächsten Tick beim Host plus bis zum nächsten Update – für ein Strategiespiel ausreichend. Die UI kann gesendete, noch nicht bestätigte Commands als „ausstehend“ anzeigen.

### 9.4 Snapshot-Größe und Deltas

- **Start:** jedes Update ist ein vollständiger Snapshot – serialisiert und komprimiert (GZip oder Brotli aus .NET).
- **Messen:** Ein Core-Test erzeugt einen Zustand in MVP-Größe (Europakarte, realistische Armeenzahl) und gibt die Snapshot-Größe roh und komprimiert aus. Im Spiel zeigt eine Debug-Anzeige Bytes pro Update und pro Sekunde.
- **Deltas** werden erst eingeführt, wenn die Messung zeigt, dass die Bandbreite (z. B. über Hamachi) nicht reicht. Die Schwelle wird festgelegt, sobald Messwerte vorliegen.
- Übertragung über einen zuverlässigen ENet-Kanal. Weil jeder Snapshot vollständig ist, ist ein späterer Wechsel auf einen unzuverlässigen Kanal möglich.

### 9.5 Abläufe

- **Beitreten:** `Hello` → Prüfung von Version und Regelwerk-Hash → `Welcome` → ab sofort `StateUpdate`s. Ein eigener Initial-Snapshot ist nicht nötig, weil jedes Update vollständig ist.
- **Pause / Geschwindigkeit:** `SessionRequest` an den Host → Session ändert Status → `SimulationDriver` hält an bzw. ändert die Rate → sofortiges Update an alle. Da nur der Host simuliert, wirken Pause und Tempo für alle gleichzeitig (Feature 2).
- **Verbindungsverlust:** Host erkennt Trennung → Session pausiert → Status „Warte auf Spieler X“ geht an alle. Der Host kann „durch KI ersetzen“ → KI erzeugt ab dann Commands für diese Nation.
- **Rückkehr:** Spieler verbindet sich mit gleichem Namen → Slot wechselt von KI zurück zum Spieler → normale Updates.

### 9.6 Aufteilung

- `Game.Core/Protocol`: Nachrichtentypen, Serialisierung, Kompression – testbar ohne Netzwerk.
- `godot/`: ENet-Verbindung, `StateBroadcaster` (Host), `ClientStateMirror` (Client), Senden/Empfangen von Bytes.

---

## 10. KI

- Lebt in `Game.Core/AI`, liest den Zustand, gibt Commands zurück. Nutzt **dieselben Commands und Validierung** wie Spieler (Feature 9).
- Läuft **nur beim Host**, z. B. alle N Ticks pro Nation (gestaffelt, um Lastspitzen zu vermeiden).
- Die KI nutzt einen **eigenen Seeded-RNG mit festem Seed**, nicht den Simulations-RNG. Ihr Zustand gehört zum Host-only-Teil des Spielstands ([Entscheidung 0002](decisions/0002-eigener-ki-zufallsgenerator.md)).
- Testbar: KI-gegen-KI-Partien headless in xUnit über viele Ticks (Smoke-Test: kein Absturz, KI baut, KI erobert) – dank festem Seed reproduzierbar.

---

## 11. Determinismus-Regeln

Determinismus wird nicht für das Netzwerk gebraucht, aber für **reproduzierbare Tests**, den Speicher-Hash-Test und das Nachspielen von Fehlern über Command-Logs.

- Keine `float`/`double` in der Simulation, keine `DateTime.Now`, kein `System.Random`, keine `Guid.NewGuid()`.
- Keine Abhängigkeit von Hash-Iterationsreihenfolge.
- Commands in fester Reihenfolge anwenden.
- Zufall nur über den Simulations-RNG bzw. den KI-RNG.
- Kein Zugriff auf Godot oder Umgebung aus Core.
- Test: gleiche Startdaten + gleiches Command-Log ⇒ gleicher Hash, zweimal hintereinander und über Speichern/Laden hinweg.

---

## 12. Meilensteine bis zum MVP

Jeder Meilenstein endet mit grünem `dotnet test` und ist entweder in Godot spielbar oder per Test prüfbar.

| # | Meilenstein | Inhalt | Prüfbar / spielbar durch |
|---|---|---|---|
| **M0** | Gerüst | Solution, Game.Core, Tests, Godot-Projekt mit Referenz auf Core. Leerer `GameState`, `Step()` erhöht Tick. RNG, Zustands-Hash, JSON-Serialisierung. Datenlader, der Inhalt entgegennimmt; Godot liest `res://data/`. | Tests: Determinismus-Hash, Serialisierungs-Roundtrip. Godot zeigt laufenden Tickzähler. |
| **M1** | Karte & Provinzen | Kartenformat in `godot/data/`, kleine **Testkarte** (~10 Provinzen). Provinzen mit Besitzer, Größe, Rohstoff, Nachbarn. Darstellung, Klick, Besitzerfarbe, Info-Panel. | Feature 1 (mit Testkarte). Tests: Daten laden/validieren, Nachbarschaft. |
| **M2** | Zeit & Commands lokal | Command-Pipeline (Envelope, Handler, Queue, Ablehnung), Session mit Pause & Geschwindigkeitsstufen, `SimulationDriver`, `IGameStateSource`. | Spielbar: pausieren, Tempo wechseln. Tests: Command-Reihenfolge, ungültige Commands. |
| **M3** | Wirtschaft I | Basis-Produktion, nationaler Pool, Steuern, Geld. Ressourcenleiste im UI. | Spielbar: Pool wächst. Tests: Produktion/Steuern über N Ticks. |
| **M4** | Speichern/Laden lokal | `SaveGame`, Versionierung, Kompression, Speichern/Laden-Menü. Bewusst früh, damit jedes weitere Feature sofort speicherbar bleibt. | Kerntest „speichern-laden ≡ durchlaufen“. Spielbar: speichern, beenden, fortsetzen. |
| **M5** | Gebäude | Bau, Bauzeit, Ausbaustufen, Wirkung (z. B. Produktionsbonus), Kosten. | Feature 4. |
| **M6** | Wirtschaft II | Fabriken mit Rezepten, Markt (Kauf/Verkauf, Preisanstieg, Erholung), fortgeschrittene Güter, ~25 Ressourcen in `godot/data/`. | Feature 3 vollständig. |
| **M7** | Armeen & Bewegung | Rekrutieren (kostet Güter), gemischte Stacks, Zusammenlegen/Teilen, Pfadsuche, Bewegung nach Provinzgröße und Einheitentyp, Interpolation zwischen Ticks. | Spielbar: Armeen über die Karte ziehen. Tests: Bewegungsdauer, Pfad. |
| **M8** | Krieg & Kampf | Diplomatie (Krieg/Frieden), automatischer Kampf in Runden, Eroberung durch Einmarsch. | Features 5 + 6. Spielbar im Sandbox-Modus mit mehreren lokal umschaltbaren Nationen. |
| **M9** | Partie-Rahmen & Sieg | Spielstart-Setup (Nationenwahl, Zeitlimit), Punkte, Sofortsieg, Zeitlimit-Sieg, Ende-Bildschirm. | Feature 7. Eine Partie hat Anfang und Ende. |
| **M10** | KI | Eigener KI-RNG. Wirtschaft aufbauen, Militär aufbauen, Krieg führen. Vor dem Multiplayer, weil der Multiplayer die KI für Disconnects braucht. | Feature 9. **Erster vollständiger Einzelspieler.** Tests: KI-gegen-KI headless, reproduzierbar. |
| **M11** | Multiplayer I | Host/Join per IP, Handshake, `StateUpdate` mit konfigurierbarer Rate, `ClientStateMirror`, Client-Interpolation, Pause/Tempo synchron, Größenmessung der Snapshots. | Feature 2 vollständig, Feature 8 Grundlage: 2 Instanzen auf einem Rechner. Test: Snapshot-Größe in MVP-Größe. |
| **M12** | Multiplayer II | Disconnect-Dialog, KI-Ersatz, Rejoin, Multiplayer-Speichern/Laden. | Features 8 + 10 vollständig. |
| **M13** | Europakarte & Balancing | Europakarte als Daten, Balancing-Durchgang (Partiedauer 3–6 h, Bewegungstempo), ggf. Deltas, falls die Messung aus M11 es verlangt. | **MVP.** Testpartie über mehrere Abende. |

Die Europakarte kann parallel ab M1 als reine Datenarbeit entstehen; bis M13 reicht die Testkarte für alles Technische.

---

## 13. Offene Punkte mit Architektur-Auswirkung

Aus den offenen Fragen in `requirements.md` – spätestens im genannten Meilenstein zu beantworten:

| Frage | Betrifft | Spätestens |
|---|---|---|
| Kartenformat / Provinz-Picking (ID-Bild vs. Polygone), Anzahl Provinzen | Data, Godot-MapView | M1 |
| Wer darf eine Pause aufheben? | Session | M2 |
| Wovon hängen Steuern ab? Bevölkerung/Moral? | State, Systems | M3 |
| Welche Landeinheiten? | Data | M7 |
| Friedensschluss einseitig oder mit Zustimmung? Truppen in fremdem Gebiet bei Frieden? | Commands, Systems | M8 |
| Startbedingungen (Nationenwahl, Startressourcen, Startarmeen) | Setup | M9 |
| Schwelle für die Einführung von Deltas | Protocol | nach Messung in M11 |
