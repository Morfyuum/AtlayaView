# AtlayaView Release Notes

## 2.0.13 - 2026-07-15

- Wichtiger Bugfix: Nach Abbruch eines Scans („Scan abbrechen") oder einem Scan-Fehler blieb
  das „Scan läuft …"-Overlay dauerhaft sichtbar, obwohl der Scan intern bereits beendet war –
  die App wirkte komplett eingefroren und ließ sich nur über den Task-Manager beenden. Per
  Live-Speicherabzug des hängenden Prozesses (`dotnet-dump`) bestätigt: `IsScanning` wurde
  intern korrekt auf `false` gesetzt, aber `PropertyChanged` feuerte wegen einer fehlenden
  expliziten `[CallerMemberName]`-Angabe in `SetScanningState()` mit dem falschen
  Property-Namen („SetScanningState" statt „IsScanning") – das Overlay reagierte dadurch
  nie auf Scan-Abbruch oder -Fehler, nur ein zufälliges gleichzeitiges Aktualisieren von
  `DisplayRoot` (nur bei erfolgreichem Scan-Abschluss) verdeckte den Bug bisher. Betraf JEDEN
  abgebrochenen oder fehlgeschlagenen Scan, nicht nur bestimmte Ordner.
- Release-Version auf 2.0.13 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.13-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.13-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.13-win-x64.zip`, `dist/AtlayaView-2.0.13-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.13-win-x64/atlaya_renderer.dll`

## 2.0.12 - 2026-07-15

- Programm-Icon ergänzt: Bisher hatte AtlayaView weder ein Windows-Programm-Icon
  (Taskleiste/Desktop/Alt-Tab/Explorer zeigten das generische .NET-Symbol) noch ein Icon im
  Leer-Zustand der App selbst (nur ein 🌲-Platzhalter-Emoji). Aus dem offiziellen Atlaya-Logo
  (`ui/static/atlaya.png`, 96×96) wurde ein Mehrfachauflösungs-`.ico`
  (`Resources/atlaya.ico`, 16/32/48/64/96 px) erzeugt und per `<ApplicationIcon>` im .csproj
  als Executable-Icon eingebunden; im Leer-Zustand ersetzt das gleiche Logo (`Resources/atlaya.png`)
  das Emoji.
- Aktive Version wird jetzt neben dem Programmnamen im Leer-Zustand angezeigt
  (`LocalizationManager.EmptyStateVersion`, alle 5 Sprachen).
- Release-Version auf 2.0.12 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.12-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.12-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.12-win-x64.zip`, `dist/AtlayaView-2.0.12-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.12-win-x64/atlaya_renderer.dll`

## 2.0.11 - 2026-07-15

- Echter Fund nach Chris' Rückmeldung „Update von 2.0.9 auf 2.0.10 geht nicht, Version
  bleibt 2.0.9": Bei Installation in einem geschützten Ordner (bei ihm
  `C:\Program Files (x86)\AtlayaView`) scheiterte `Copy-Item` im Austausch-Skript lautlos an
  „Access Denied", weil das Skript ohne Administratorrechte lief – AtlayaView startete danach
  einfach unverändert neu, ohne jede Fehlermeldung. Fix: `SelfUpdater.LaunchSwapAndExit()`
  prüft jetzt per Schreibprobe, ob der Installationsordner beschreibbar ist; falls nicht,
  startet das Austausch-Skript mit `Verb=runas` (UAC-Sicherheitsabfrage). Fehler landen
  zusätzlich in `%TEMP%\AtlayaView_apply.log`, und die App startet auch bei einem Fehler
  wieder (statt spurlos zu verschwinden). Bricht der Nutzer die UAC-Abfrage ab, zeigt der
  Update-Dialog jetzt eine klare Meldung statt eines generischen Download-Fehlers.
- Release-Version auf 2.0.11 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.11-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.11-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.11-win-x64.zip`, `dist/AtlayaView-2.0.11-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.11-win-x64/atlaya_renderer.dll`

## 2.0.10 - 2026-07-15

- Italienische Sprachauswahl + Übersetzung ergänzt: `AppLanguage`-Enum hatte bisher nur
  Deutsch/Englisch/Französisch/Spanisch, obwohl die Webseite und alle anderen Atlaya-
  Produkte seit Langem 5-sprachig (DE/EN/FR/IT/ES) sind. `Core/LocalizationManager.cs`
  übersetzt jetzt alle ~90 UI-Texte (Menüs, Dialoge, Statusmeldungen, Update-Bereich)
  zusätzlich ins Italienische, neuer Menüpunkt „Italiano" unter Ansicht → Sprache.
- Release-Version auf 2.0.10 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.10-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.10-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.10-win-x64.zip`, `dist/AtlayaView-2.0.10-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.10-win-x64/atlaya_renderer.dll`

## 2.0.9 - 2026-07-15

- Echter Fund nach Chris' Rückmeldung, dass sich in den Options-Dialoge weder
  „Update-Prüfung" noch „Prüfabstand" öffnen/ändern ließen (2.0.8 hatte nur die
  Sichtbarkeit gefixt, nicht die eigentliche Ursache): das globale `ComboBox`-Template in
  `Resources/Styles.xaml` hatte **keinen `ToggleButton`** – `IsDropDownOpen` wurde nirgends
  gesetzt, das Dropdown öffnete sich bei Mausklick nie. Betraf damit potenziell JEDE
  ComboBox der App (`cmbDrives`, `cmbOpener`, `cmbSizeUnit`, nicht nur die neuen
  Update-Felder), nur bisher nicht aufgefallen. Fix: Template um einen echten
  `ToggleButton` (an `IsDropDownOpen` zweiseitig gebunden) ergänzt, exakt gleiches
  Aussehen wie zuvor.
- Release-Version auf 2.0.9 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.9-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.9-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.9-win-x64.zip`, `dist/AtlayaView-2.0.9-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.9-win-x64/atlaya_renderer.dll`

## 2.0.8 - 2026-07-15

- Bugfix Options-Dialog: Die neue „Updates"-Sektion (seit 2.0.7) war am unteren Rand des
  Dialogs nicht erreichbar, weil der Inhalt bei fester Fensterhöhe (620px, kein Scrollen)
  nicht mehr vollständig hineinpasste – die automatischen Prüfmodi + Intervall-Auswahl waren
  dadurch faktisch nicht auswählbar. Fix: Einstellungsinhalt jetzt in einem `ScrollViewer`,
  Fenster zusätzlich per Ziehgriff vergrößerbar (`ResizeMode="CanResizeWithGrip"`).
- Release-Version auf 2.0.8 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.8-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.8-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.8-win-x64.zip`, `dist/AtlayaView-2.0.8-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.8-win-x64/atlaya_renderer.dll`

## 2.0.7 - 2026-07-14

- Neue Update-Prüfung eingebaut (erste Netzwerkfunktion neben den Impressum/Datenschutz-
  Links): `Core/UpdateChecker.cs` fragt `atlayaview/updates/latest.json` auf
  atlaya.capecter.com ab und vergleicht die Version.
- Echter selbstersetzender Updater (`Core/SelfUpdater.cs`): lädt die passende Variante
  (mit/ohne .NET, automatisch anhand der installierten Variante erkannt) in einen
  Temp-Ordner, erzeugt ein PowerShell-Hilfsskript, das nach Prozessende die Dateien
  austauscht und AtlayaView neu startet.
- Hilfe-Menü um "Auf Updates prüfen …" erweitert (`Dialogs/UpdateDialog`), zeigt
  installierte/neue Version + Hinweistext, Button "Jetzt aktualisieren" nur bei
  gefundener passender Variante.
- Options-Dialog um zwei neue Auswahlfelder erweitert: Prüfmodus (Manuell / Automatisch
  prüfen / Automatisch prüfen und updaten) und Intervall (Täglich/Wöchentlich/Monatlich/
  Jährlich), persistiert in `AtlayaView.settings.json`.
- `Core/UpdateScheduler.cs` prüft im Hintergrund (30-Minuten-Poll, eigener Zeitstempel in
  `update_check_state.json`) und wendet je nach Modus automatisch an oder benachrichtigt nur.
- Release-Version auf 2.0.7 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.7-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.7-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.7-win-x64.zip`, `dist/AtlayaView-2.0.7-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.7-win-x64/atlaya_renderer.dll`

## 2.0.6 - 2026-07-14

- Hilfe-Menü um "Impressum (Web)" und "Datenschutz (Web)" erweitert – öffnet die
  entsprechenden Seiten auf atlaya.capecter.com im Standardbrowser (einziger Netzwerkzugriff
  der Anwendung, nur auf ausdrücklichen Klick).
- Info-Dialog ("Über AtlayaView") ergänzt um einen kurzen Datenschutz-Hinweis: die
  Anwendung verarbeitet keine personenbezogenen Daten und stellt sonst keine
  Netzwerkverbindung her.
- Release-Version auf 2.0.6 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.6-win-x64/AtlayaView.exe` (mit .NET, self-contained)
- EXE: `dist/publish/AtlayaView-2.0.6-win-x64-fx/AtlayaView.exe` (ohne .NET, framework-dependent)
- ZIP: `dist/AtlayaView-2.0.6-win-x64.zip`, `dist/AtlayaView-2.0.6-win-x64-fx.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.6-win-x64/atlaya_renderer.dll`

## 2.0.5 - 2026-05-14

- Scan-Overlay oeffnet jetzt unmittelbar beim Scanstart im Hauptfenster.
- Der erste Scan-Schritt zeigt das Einlesen mit sichtbarer Balkenbewegung statt mit einem statischen Nullstand.
- Direkt danach schaltet der Ablauf ohne Zusatzfenster in die Verarbeitungsphase mit echtem Fortschritt um.
- Der untere Button "Scan abbrechen" ist direkt an den aktiven Scan gekoppelt.
- Release-Version auf 2.0.5 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.5-win-x64/AtlayaView.exe`
- ZIP: `dist/AtlayaView-2.0.5-win-x64.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.5-win-x64/atlaya_renderer.dll`

## 2.0.4 - 2026-05-14

- Toolbar erweitert um einen direkten Laufwerks-Picker fuer Einzel- und Mehrfachauswahl.
- Laufwerksmenues bleiben bei Mehrfachauswahl offen, damit mehrere Haken ohne staendiges Neuoeffnen gesetzt werden koennen.
- Auswahl im Toolbar-Picker und im Ansicht-Menue bleibt synchron.
- Release-Version auf 2.0.4 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.4-win-x64/AtlayaView.exe`
- ZIP: `dist/AtlayaView-2.0.4-win-x64.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.4-win-x64/atlaya_renderer.dll`

## 2.0.3 - 2026-05-14

- Laufwerksauswahl aus dem Ansicht-Menü startet Scans jetzt automatisch ohne zusätzlichen F5-Refresh.
- Einzelne Menü-Auswahl startet nach kurzer Verzögerung direkt den Scan des gewählten Laufwerks.
- Mehrfachauswahl startet parallele Laufwerks-Scans automatisch und verwirft überholte Zwischenläufe über eine Generation-Schranke.
- Release-Version auf 2.0.3 angehoben in .NET-Projekt, Manifest und Rust-Renderer.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.3-win-x64/AtlayaView.exe`
- ZIP: `dist/AtlayaView-2.0.3-win-x64.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.3-win-x64/atlaya_renderer.dll`

## 2.0.2 - 2026-05-14

- App-Version auf 2.0.2 angehoben in .NET-Projekt, Manifest und Rust-Renderer.
- Über-Dialog erweitert: zeigt jetzt die echte App-Version sowie Informationen zur Rust-Integration und zum Fallback auf den verwalteten C#-Renderer.
- Nativer Renderer exportiert seine eigene Versionsnummer über `atlaya_renderer_version`, damit die UI den Rust-Stand direkt anzeigen kann.
- Release-Build und Packaging über `build-hybrid.ps1` stabilisiert.

## Artefakte

- EXE: `dist/publish/AtlayaView-2.0.2-win-x64/AtlayaView.exe`
- ZIP: `dist/AtlayaView-2.0.2-win-x64.zip`
- Native DLL: `dist/publish/AtlayaView-2.0.2-win-x64/atlaya_renderer.dll`

## Build und Verifikation

- Rust-Validierung: `cargo check` in `native/atlaya_renderer` erfolgreich.
- Release-Publish: `pwsh -File .\build-hybrid.ps1 -Configuration Release` erfolgreich.
- Das Publish-Skript baut zuerst den Rust-Renderer mit `cargo build --release` und veröffentlicht danach die WPF-Anwendung nach `dist/publish/AtlayaView-<Version>-win-x64`.
- Im Anschluss wird automatisch ein versionsiertes ZIP unter `dist/` erzeugt.

## Technische Hinweise

- `AtlayaView.csproj` schließt generierte Inhalte unter `obj`, `bin`, `dist/publish` und `.artifacts` explizit aus, um WPF-Doppeldefinitionen aus `wpftmp`-Zwischenständen zu verhindern.
- `build-hybrid.ps1` nutzt für Publish-Zwischenartefakte einen Temp-Pfad außerhalb des Repos, um Konflikte mit Security-Scannern auf `obj/.../refint/*.dll` zu vermeiden.