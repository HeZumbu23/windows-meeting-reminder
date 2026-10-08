# Meeting Reminder (Task-Tray-App)

Windows-11-Anwendung, die im System-Tray läuft und bei jedem fälligen Meeting
ein großes, auffälliges Vollbild-Popup zeigt. Ersetzt die vorherige
PowerShell/Aufgabenplanung-Lösung vollständig – kein Konsolenfenster, keine
geplante Aufgabe, kein sichtbares Hauptfenster im Normalbetrieb.

## Funktionsumfang

- **Tray-Icon** (rote Uhr) unten rechts neben der Systemuhr, kein Fenster beim
  Start. Dasselbe Design zeigt auch die .exe selbst im Explorer/Taskleiste.
- **Rechtsklick-Menü**: Termine bearbeiten, Termindatei im Explorer anzeigen,
  Einstellungen, Autostart an/aus, Beenden. Doppelklick auf das Icon öffnet
  ebenfalls die Terminbearbeitung.
- **Terminverwaltung** über einen Tabellen-Dialog (Wochentag, Uhrzeit,
  Rhythmus, Titel); die Daten liegen zusätzlich als lesbare JSON-Datei
  `MeetingReminder/meetings.json` (neben `MeetingReminder.csproj`) und können
  bei Bedarf auch direkt editiert werden. Die Datei ist bewusst
  **eingecheckt** - Terminänderungen lassen sich damit ganz normal per
  `git diff`/`git commit` nachvollziehen und versionieren.
- **Wöchentliche oder 2-wöchentliche Termine**: Standardmäßig feuert ein
  Termin jede Woche am gewählten Wochentag. Bei Rhythmus "Alle 2 Wochen"
  wird zusätzlich ein Startdatum angegeben (z.B. `13.10.2026`) – ab dann
  feuert der Termin alle 14 Tage an diesem Wochentag.
- **Zwei gestaffelte Vollbild-Popups** vor jedem Termin, unabhängig
  voneinander konfigurierbar über **Rechtsklick → "Einstellungen..."**:
  - **Grün** (früher, unaufdringlicherer Hinweis): standardmäßig
    **5 Minuten vorher**.
  - **Rot** (dringlich, kurz vorm Start): standardmäßig **30 Sekunden
    vorher**.

  Beide zeigen Titel und Startzeit, die Kopfzeile rechnet den tatsächlichen
  Abstand live aus (z.B. "MEETING IN 30 SEKUNDEN!" oder "MEETING IN
  5 MINUTEN!"). Schließbar per Klick, ESC oder Enter, mit "5 Minuten später
  erinnern" (Snooze, feuert dann als rote Erinnerung) und automatischem
  Schließen nach 90 Sekunden. Eine Vorlaufzeit auf 0 setzen deaktiviert die
  jeweilige Stufe.
- **Autostart** über den `HKCU\...\CurrentVersion\Run`-Registry-Schlüssel,
  umschaltbar direkt im Tray-Menü (kein Admin-Recht nötig).
- **Zuverlässige Prüfung** alle 20 Sekunden mit 5-Minuten-Gnadenfrist ab dem
  jeweiligen Erinnerungszeitpunkt (Termin minus grüner bzw. roter
  Vorlaufzeit), damit ein Termin auch nach einer kurzen Standby-Phase noch
  zuverlässig gemeldet wird. Es werden keine anwachsenden Datenstrukturen
  gehalten (pro Tag zurückgesetzte "bereits ausgelöst"-Liste je Stufe),
  damit die App auch über Tage hinweg ohne Speicherzuwachs läuft.
- **Eine Instanz gleichzeitig**: Ein zweiter Start (z.B. durch Autostart und
  manuellen Doppelklick) zeigt nur einen Hinweis und beendet sich sofort.
- **Sauberes Beenden**: "Beenden" im Tray-Menü entfernt das Icon und stoppt
  den Prozess vollständig, es bleibt nichts im Hintergrund hängen. Die App
  hat bewusst kein Konsolenfenster - Strg+C/Strg+Pause funktionieren daher
  nicht wie bei einer Konsolenanwendung; zum Beenden bitte den Tray-Menüpunkt
  nutzen (siehe "Strg+C schließt die App nicht" weiter unten).

## Projektstruktur

```
MeetingReminder.sln
deploy.ps1                     Baut/startet neu, sobald neue Commits landen (siehe "Deploy-Skript")
MeetingReminder/
  MeetingReminder.csproj       Projektdatei (.NET 8, WinForms, net8.0-windows)
  meetings.json                 Terminplan (eingecheckt, siehe "Termine anpassen")
  settings.json                 Vorlaufzeiten grün/rot (eingecheckt, siehe "Einstellungen")
  AppIcon.ico                   Icon der .exe (Explorer/Taskleiste), mehrere Auflösungen
  app.manifest                  DPI-Awareness-Manifest
  Program.cs                    Einstiegspunkt, Single-Instance-Schutz, Logging
  TrayApplicationContext.cs     Tray-Icon, Menü, Verdrahtung aller Teile
  Models/Meeting.cs              Termin-Datenmodell (Wochentag, Uhrzeit, Titel, Rhythmus)
  Models/ReminderSettings.cs     Vorlaufzeiten-Datenmodell (grün/rot, in Sekunden)
  Models/ReminderStage.cs        Enum: Green/Red
  Services/MeetingStore.cs       Laden/Speichern der meetings.json
  Services/SettingsStore.cs      Laden/Speichern der settings.json
  Services/DataDirectoryResolver.cs Gemeinsame Pfadsuche für beide Stores
  Services/ReminderScheduler.cs  Polling-Timer, Fälligkeitsprüfung je Stufe, Snooze
  Services/AutostartManager.cs  Registry-Autostart an/aus
  Services/TrayIconFactory.cs   Zeichnet das Tray-Icon zur Laufzeit
  Native/NativeMethods.cs       DestroyIcon-Interop (verhindert Handle-Leak)
  Forms/ReminderPopupForm.cs    Vollbild-Erinnerungs-Popup (grün/rot)
  Forms/EditMeetingsForm.cs     Tabellen-Dialog zur Terminbearbeitung
  Forms/SettingsForm.cs         Dialog für die Vorlaufzeiten
```

## Voraussetzungen zum Bauen

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows Desktop-Workload)

Dieses Repository wurde in einer Linux-Umgebung ohne .NET-SDK erstellt, der
Code konnte hier deshalb **nicht kompiliert** werden. Bitte vor dem
produktiven Einsatz einmal `dotnet build` auf einem Windows-Rechner
ausführen und die App kurz durchklicken (Tray-Menü, Terminbearbeitung,
Popup, Autostart-Umschalter).

## Bauen & Ausführen

```powershell
# Im Repository-Wurzelverzeichnis
dotnet build

# Direkt starten (zeigt beim Debuggen ein Konsolenfenster, im Release-Build nicht)
dotnet run --project MeetingReminder
```

### Strg+C schließt die App nicht

Das ist erwartetes Verhalten, kein Bug: Die App ist eine reine GUI-Anwendung
ohne eigenes Konsolenfenster (`OutputType=WinExe`) - genau das war ja
gewünscht ("kein Konsolenfenster im Normalbetrieb"). Eine fertige .exe oder
eine per Autostart gestartete Instanz hat gar keine Konsole, die Strg+C/
Strg+Pause empfangen könnte. Auch beim Starten über `dotnet run` im Terminal
läuft die eigentliche App als vom SDK-Wrapper entkoppelter Prozess - Strg+C
dort beendet meist nur `dotnet run` selbst.

Zum Beenden während der Entwicklung:

- **Empfohlen**: Rechtsklick auf das Tray-Icon → "Beenden".
- Falls das Signal den Prozess doch erreicht (abhängig vom Terminal), fängt
  die App Strg+C/Strg+Pause zusätzlich ab und fährt darüber genauso sauber
  herunter wie über "Beenden" (`Program.cs`, `Console.CancelKeyPress`).
- Alternative über die Kommandozeile: `Stop-Process -Name MeetingReminder`
  (PowerShell) bzw. `taskkill /IM MeetingReminder.exe /F` (cmd).

## Veröffentlichen als eigenständige .exe

Einzelne, eigenständige .exe-Datei (self-contained, single-file, win-x64)
ohne separat installiertes .NET - die nötigen Eigenschaften dafür
(`RuntimeIdentifier`, `SelfContained`, `PublishSingleFile`,
`IncludeNativeLibrariesForSelfExtract`) sind fest in
`MeetingReminder.csproj` hinterlegt, ein normales Publish reicht:

```powershell
dotnet publish MeetingReminder -c Release
```

Die fertige Datei liegt danach unter
`MeetingReminder\bin\Release\net8.0-windows\win-x64\publish\MeetingReminder.exe`.
Diese .exe kann direkt gestartet werden; für Autostart einfach im Tray-Menü
"Automatisch mit Windows starten" aktivieren (legt einen Registry-Eintrag mit
dem vollständigen Pfad zu dieser .exe an – die Datei sollte also an einem
festen Ort liegen bleiben, z.B. `C:\Tools\MeetingReminder\`).

Wird die veröffentlichte .exe an einen Ort **außerhalb** dieses Repositories
kopiert (z.B. nach `C:\Tools\MeetingReminder\`), findet sie dort kein
`MeetingReminder.csproj` mehr und legt `meetings.json` stattdessen direkt
neben sich selbst ab - dann eben nicht mehr eingecheckt, sondern wie eine
normale lokale Konfigurationsdatei. Für den eingecheckten Terminplan die App
also direkt aus dem Repository-Checkout heraus bauen/starten (`dotnet run`
bzw. die .exe aus `bin\...\` heraus), nicht an einen anderen Ort kopieren.

### Deploy-Skript

Für die lokale Entwicklung: `deploy.ps1` (im Repository-Wurzelverzeichnis,
gleiches Muster wie im Schwester-Repo
[taskbarlauncher](https://github.com/HeZumbu23/taskbarlauncher)) baut und
startet einmal sofort, beobachtet danach den aktuellen Branch und
deployt automatisch neu, sobald neue Commits landen - kein manuelles
"pull, neu bauen, neu starten" mehr nötig.

```powershell
.\deploy.ps1                      # beobachten, alle 60s prüfen (Standard)
.\deploy.ps1 -IntervalSeconds 15  # öfter prüfen
.\deploy.ps1 -Once                # einmal sofort deployen, ohne Beobachtungsschleife
```

Bei jedem Intervall wird nur `git fetch` ausgeführt; erst wenn der
Remote-Branch wirklich neue Commits hat, wird die laufende Instanz beendet
(`Stop-Process -Force`, kein sauberes "Beenden" über das Tray-Menü - wie im
Original), `git pull` + `dotnet publish MeetingReminder -c Release`
ausgeführt und die neue .exe gestartet. Ohne neue Commits passiert beim
Intervall nichts. Mit Strg+C beenden.

## Termine anpassen

Am einfachsten über **Rechtsklick auf das Tray-Icon → "Termine bearbeiten..."**:
Wochentag per Dropdown wählen, Uhrzeit im Format `HH:mm` eintragen, Titel
eingeben, mit "Speichern" übernehmen. Eine leere Zeile am Ende der Tabelle
legt automatisch einen neuen Termin an. Zum Löschen eine Zeile anklicken
(reicht eine einzelne Zelle) und auf **"Zeile löschen"** klicken.

Für einen **2-wöchentlichen Termin**: bei "Rhythmus" → "Alle 2 Wochen"
wählen und im Feld "Ab Datum" das erste Vorkommen im Format `TT.MM.JJJJ`
eintragen (z.B. `13.10.2026`). Der Wochentag wird dann automatisch aus
diesem Datum übernommen; ab diesem Tag feuert der Termin alle 14 Tage.

Alternativ über **"Termindatei im Explorer anzeigen"** direkt die Datei
`MeetingReminder/meetings.json` mit einem Texteditor bearbeiten, z.B.:

```json
[
  { "Day": "Monday", "Hour": 9, "Minute": 0, "Title": "Daily Standup" },
  { "Day": "Wednesday", "Hour": 15, "Minute": 0, "Title": "Sprint Review" },
  {
    "Day": "Thursday",
    "Hour": 14,
    "Minute": 0,
    "Title": "Retro",
    "IntervalWeeks": 2,
    "StartDate": "2026-10-01"
  }
]
```

`IntervalWeeks` fehlt bzw. ist `1` für ganz normale wöchentliche Termine;
`StartDate` wird dann ignoriert. Änderungen an der Datei werden beim
nächsten Start der App bzw. beim nächsten Öffnen des Bearbeiten-Dialogs
übernommen.

## Einstellungen (Vorlaufzeiten)

Über **Rechtsklick auf das Tray-Icon → "Einstellungen..."**: zwei Felder,
"Grüner Hinweis" in Minuten und "Roter Hinweis" in Sekunden vor dem Termin.
Mit "Speichern" werden sie sofort wirksam (keine neue Terminprüfung nötig)
und in `MeetingReminder/settings.json` abgelegt - genau wie `meetings.json`
eingecheckt und damit versionierbar:

```json
{
  "GreenLeadSeconds": 300,
  "RedLeadSeconds": 30
}
```

0 deaktiviert die jeweilige Stufe. Die beiden Vorlaufzeiten sind komplett
unabhängig voneinander; welche zuerst feuert, hängt allein davon ab, welche
den größeren Wert hat (standardmäßig grün 300s vor rot 30s).

## Design-Entscheidungen (Kontext für spätere Änderungen)

- **C#/WinForms mit `NotifyIcon`** statt Python/Electron: nativste, ressourcen-
  schonendste Option für eine reine Tray-App unter Windows, einfachste
  Autostart- und Vollbild-Popup-Kontrolle.
- **Polling statt akkumulierender Timer-Logik**: Die Fälligkeitsprüfung
  vergleicht bei jedem Tick die tatsächliche Systemzeit gegen den Terminplan
  statt Tick-Zähler zu addieren. Dadurch spielt ein Standby/Resume-Zyklus
  keine Rolle – nach dem Aufwachen läuft der nächste reguläre Tick einfach
  weiter, ergänzt um eine 5-Minuten-Gnadenfrist für Termine, die genau
  während einer kurzen Standby-Phase fällig wurden.
- **Tray-Icon wird zur Laufzeit gezeichnet** statt aus einer Datei geladen,
  damit die eigentliche Programmlogik ohne Binärdatei auskommt. Für das
  Datei-Icon (Explorer, Taskleiste) liegt zusätzlich `AppIcon.ico` im Repo
  und ist über `<ApplicationIcon>` in `MeetingReminder.csproj` eingebunden -
  optisch identisches Design (rote Uhr), aber ein eigener, waschechter
  Icon-Datensatz mit mehreren Auflösungen, wie Windows ihn für die .exe
  selbst braucht.
- **`meetings.json` und `settings.json` liegen im Repo statt unter
  `%AppData%`**: Beide Stores teilen sich `DataDirectoryResolver`, der
  ausgehend vom Ausführungsverzeichnis nach oben nach
  `MeetingReminder.csproj` sucht und die Dateien im gefundenen
  Projektverzeichnis ablegt, damit Änderungen über die App direkt als
  Git-Diff sichtbar und eincheckbar sind. Ohne gefundene `.csproj`
  (eigenständig veröffentlichte .exe) fällt es auf das Verzeichnis neben der
  .exe zurück.
- **Grün/Rot als zwei unabhängige Erinnerungsstufen statt einer einzelnen
  Vorlaufzeit**: `ReminderScheduler` prüft pro Tick für jeden Termin beide
  Stufen getrennt (eigene Fälligkeits- und Dedupe-Prüfung je Stufe), damit
  sie zuverlässig unabhängig voneinander und auch bei sehr unterschiedlichen
  Vorlaufzeiten (z.B. 30s vs. 5min) sauber feuern. Ein Snooze re-feuert
  immer als rote (dringliche) Erinnerung, unabhängig davon, welche Stufe ihn
  ausgelöst hat.
- **`deploy.ps1` 1:1 vom Schwester-Repo
  [taskbarlauncher](https://github.com/HeZumbu23/taskbarlauncher) übernommen**
  (nur Prozess-/Exe-Name und Publish-Pfad angepasst), damit beide Projekte
  denselben Entwickler-Workflow benutzen. Damit `dotnet publish
  MeetingReminder -c Release` ohne zusätzliche `-p:`-Flags dieselbe
  self-contained single-file .exe erzeugt, die das Skript erwartet, sind
  `RuntimeIdentifier`/`SelfContained`/`PublishSingleFile`/
  `IncludeNativeLibrariesForSelfExtract` jetzt fest in
  `MeetingReminder.csproj` hinterlegt statt nur als CLI-Flags dokumentiert.
- Die alte PowerShell/Aufgabenplanung-Lösung (`meetings.csv`, `reminder.ps1`,
  `setup_task.ps1`, `remove_task.ps1`) ist **nicht** Teil dieses Repos – das
  Repository war zu Beginn dieser Aufgabe leer, es gab daher nichts zu
  ersetzen oder zu migrieren.
