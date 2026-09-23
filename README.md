# Meeting Reminder (Task-Tray-App)

Windows-11-Anwendung, die im System-Tray läuft und bei jedem fälligen Meeting
ein großes, auffälliges Vollbild-Popup zeigt. Ersetzt die vorherige
PowerShell/Aufgabenplanung-Lösung vollständig – kein Konsolenfenster, keine
geplante Aufgabe, kein sichtbares Hauptfenster im Normalbetrieb.

## Funktionsumfang

- **Tray-Icon** (rote Uhr) unten rechts neben der Systemuhr, kein Fenster beim Start.
- **Rechtsklick-Menü**: Termine bearbeiten, Termindatei im Explorer anzeigen,
  Autostart an/aus, Beenden. Doppelklick auf das Icon öffnet ebenfalls die
  Terminbearbeitung.
- **Terminverwaltung** über einen Tabellen-Dialog (Wochentag, Uhrzeit, Titel);
  die Daten liegen zusätzlich als lesbare JSON-Datei unter
  `%AppData%\MeetingReminder\meetings.json` und können bei Bedarf auch direkt
  editiert werden.
- **Vollbild-Popup** (rot, große Schrift) beim Erreichen eines Termins –
  schließbar per Klick, ESC oder Enter, mit "5 Minuten später erinnern"
  (Snooze) und automatischem Schließen nach 90 Sekunden.
- **Autostart** über den `HKCU\...\CurrentVersion\Run`-Registry-Schlüssel,
  umschaltbar direkt im Tray-Menü (kein Admin-Recht nötig).
- **Zuverlässige Prüfung** alle 20 Sekunden mit 5-Minuten-Gnadenfrist ab dem
  geplanten Zeitpunkt, damit ein Termin auch nach einer kurzen Standby-Phase
  noch zuverlässig gemeldet wird. Es werden keine anwachsenden Datenstrukturen
  gehalten (pro Tag zurückgesetzte "bereits ausgelöst"-Liste), damit die App
  auch über Tage hinweg ohne Speicherzuwachs läuft.
- **Eine Instanz gleichzeitig**: Ein zweiter Start (z.B. durch Autostart und
  manuellen Doppelklick) zeigt nur einen Hinweis und beendet sich sofort.
- **Sauberes Beenden**: "Beenden" im Tray-Menü entfernt das Icon und stoppt
  den Prozess vollständig, es bleibt nichts im Hintergrund hängen.

## Projektstruktur

```
MeetingReminder.sln
MeetingReminder/
  MeetingReminder.csproj      Projektdatei (.NET 8, WinForms, net8.0-windows)
  app.manifest                 DPI-Awareness-Manifest
  Program.cs                   Einstiegspunkt, Single-Instance-Schutz, Logging
  TrayApplicationContext.cs    Tray-Icon, Menü, Verdrahtung aller Teile
  Models/Meeting.cs            Termin-Datenmodell (Wochentag, Uhrzeit, Titel)
  Services/MeetingStore.cs     Laden/Speichern der meetings.json
  Services/ReminderScheduler.cs Polling-Timer, Fälligkeitsprüfung, Snooze
  Services/AutostartManager.cs Registry-Autostart an/aus
  Services/TrayIconFactory.cs  Zeichnet das Tray-Icon zur Laufzeit
  Native/NativeMethods.cs      DestroyIcon-Interop (verhindert Handle-Leak)
  Forms/ReminderPopupForm.cs   Vollbild-Erinnerungs-Popup
  Forms/EditMeetingsForm.cs    Tabellen-Dialog zur Terminbearbeitung
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

## Veröffentlichen als eigenständige .exe

Für eine einzelne, verteilbare .exe ohne separat installiertes .NET:

```powershell
dotnet publish MeetingReminder -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Die fertige Datei liegt danach unter
`MeetingReminder\bin\Release\net8.0-windows\win-x64\publish\MeetingReminder.exe`.
Diese .exe kann direkt gestartet werden; für Autostart einfach im Tray-Menü
"Automatisch mit Windows starten" aktivieren (legt einen Registry-Eintrag mit
dem vollständigen Pfad zu dieser .exe an – die Datei sollte also an einem
festen Ort liegen bleiben, z.B. `C:\Tools\MeetingReminder\`).

## Termine anpassen

Am einfachsten über **Rechtsklick auf das Tray-Icon → "Termine bearbeiten..."**:
Wochentag per Dropdown wählen, Uhrzeit im Format `HH:mm` eintragen, Titel
eingeben, mit "Speichern" übernehmen. Eine leere Zeile am Ende der Tabelle
legt automatisch einen neuen Termin an.

Alternativ über **"Termindatei im Explorer anzeigen"** direkt die Datei
`meetings.json` mit einem Texteditor bearbeiten, z.B.:

```json
[
  { "Day": "Monday", "Hour": 9, "Minute": 0, "Title": "Daily Standup" },
  { "Day": "Wednesday", "Hour": 15, "Minute": 0, "Title": "Sprint Review" }
]
```

Änderungen an der Datei werden beim nächsten Start der App bzw. beim
nächsten Öffnen des Bearbeiten-Dialogs übernommen.

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
- **Tray-Icon wird zur Laufzeit gezeichnet** (kein `.ico`-Asset im Repo), um
  keine Binärdatei pflegen zu müssen.
- Die alte PowerShell/Aufgabenplanung-Lösung (`meetings.csv`, `reminder.ps1`,
  `setup_task.ps1`, `remove_task.ps1`) ist **nicht** Teil dieses Repos – das
  Repository war zu Beginn dieser Aufgabe leer, es gab daher nichts zu
  ersetzen oder zu migrieren.
